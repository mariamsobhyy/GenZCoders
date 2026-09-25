import type { MaterialDto } from 'src/api/models/material';

import { useTranslation } from 'react-i18next';
import ZoomMtgEmbedded from '@zoom/meetingsdk/embedded';
import { useRef, useState, useEffect, useCallback } from 'react';

import { alpha, useTheme } from '@mui/material/styles';
import {
  Box,
  Button,
  Dialog,
  IconButton,
  Typography,
  DialogTitle,
  DialogContent,
  CircularProgress,
} from '@mui/material';

import { openSafeUrl } from 'src/utils/safe-url';

import { zoomApi } from 'src/api';

import { Iconify } from 'src/components/iconify';

const ZOOM_ROOT_ID = 'zoom-meeting-root';

type EmbeddedClient = ReturnType<typeof ZoomMtgEmbedded.createClient>;

interface ZoomMeetingViewerProps {
  meeting: MaterialDto | null;
  open: boolean;
  onClose: () => void;
  isHost?: boolean;
}

export function ZoomMeetingViewer({ meeting, open, onClose, isHost = false }: ZoomMeetingViewerProps) {
  const { t } = useTranslation();
  const theme = useTheme();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Keep the latest onClose without re-running the join effect on every parent render
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  const handleJoinInBrowser = useCallback(() => {
    if (!meeting?.link) return;

    let joinUrl: string;
    try {
      const url = new URL(meeting.link);
      if (meeting.meetingPassword) {
        url.searchParams.set('pwd', meeting.meetingPassword);
      }
      joinUrl = url.toString();
    } catch {
      setError(t('student.connectionFailed'));
      return;
    }

    if (!openSafeUrl(joinUrl)) {
      setError(t('student.connectionFailed'));
    }
  }, [meeting, t]);

  useEffect(() => {
    if (!open || !meeting) {
      setLoading(true);
      setError(null);
      return () => {};
    }

    if (!meeting.meetingId && !meeting.link) {
      setError(t('student.connectionFailed'));
      setLoading(false);
      return () => {};
    }

    const currentMeeting = meeting;
    const meetingNumber = currentMeeting.meetingId;
    let cancelled = false;
    let client: EmbeddedClient | null = null;

    const handleConnectionChange = (payload: { state?: string }) => {
      // Meeting ended / host removed us → close the dialog
      if (!cancelled && payload?.state === 'Closed') {
        onCloseRef.current();
      }
    };

    const runJoin = async () => {
      try {
        setLoading(true);
        setError(null);

        const { sdkAvailable } = await zoomApi.getSdkAvailable();

        if (cancelled) return;

        if (!sdkAvailable || !meetingNumber) {
          setLoading(false);
          setError(currentMeeting.link
            ? t('student.joinInBrowserMessage')
            : t('student.connectionFailed'));
          return;
        }

        try {
          client = ZoomMtgEmbedded.createClient();
        } catch (e) {
          console.error('Failed to create Zoom client', e);
          client = null;
        }

        if (!client) {
          setError('Zoom SDK unavailable');
          setLoading(false);
          return;
        }

        const sigRes = await zoomApi.getSignature(meetingNumber, isHost ? 1 : 0);
        const signature = sigRes.signature;
        const sdkKey = sigRes.sdkKey;

        if (cancelled) return;

        const element = document.getElementById(ZOOM_ROOT_ID);
        if (!element) {
          throw new Error('Zoom meeting container not found');
        }

        await client.init({
          zoomAppRoot: element,
          language: 'en-US',
          patchJsMedia: true,
        });

        if (cancelled) return;

        // @zoom/meetingsdk@5.x embedded join requires sdkKey
        await client.join({
          signature,
          sdkKey: sdkKey || '',
          meetingNumber: String(meetingNumber),
          password: currentMeeting.meetingPassword || '',
          userName: isHost ? (currentMeeting.instructorName || 'Host') : 'Student',
        });

        if (cancelled) return;

        client.on('connection-change', handleConnectionChange);
        setLoading(false);
      } catch (e: any) {
        if (cancelled) return;
        console.error('Zoom join failed:', e);
        const errMsg =
          e?.data?.message ||
          e?.response?.data?.message ||
          e?.message ||
          (typeof e === 'string' ? e : null) ||
          (() => {
            try {
              return JSON.stringify(e);
            } catch {
              return null;
            }
          })() ||
          t('student.connectionFailed');
        setError(typeof errMsg === 'string' && errMsg.trim() ? errMsg : t('student.connectionFailed'));
        setLoading(false);
      }
    };

    runJoin();
    return () => {
      cancelled = true;
      const activeClient = client;
      if (!activeClient) return;

      try {
        activeClient.off('connection-change', handleConnectionChange);
      } catch {
        // ignore
      }

      // Leave the meeting (no-op rejection if never joined) and tear down the
      // embedded client so the next open starts clean on the new DOM container.
      Promise.resolve()
        .then(() => activeClient.leaveMeeting())
        .catch(() => {
          // not joined / already left
        })
        .finally(() => {
          try {
            ZoomMtgEmbedded.destroyClient();
          } catch {
            // ignore
          }
        });
    };
  }, [open, meeting?.id, meeting?.meetingId, meeting?.meetingPassword, meeting?.link, meeting?.instructorName, isHost, t]);

  if (!meeting) return null;

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="lg"
      fullWidth
      disableEnforceFocus
      disableAutoFocus
      disableRestoreFocus
      PaperProps={{
        sx: {
          borderRadius: 3,
          overflow: 'hidden',
          bgcolor: 'background.default',
          minHeight: 500,
        },
      }}
    >
      <DialogTitle
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          bgcolor: alpha(theme.palette.primary.main, 0.05),
          borderBottom: `1px solid ${theme.palette.divider}`,
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Iconify icon="solar:video-bold" sx={{ color: 'primary.main' }} />
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            {isHost ? `Host: ${meeting.title}` : meeting.title}
          </Typography>
        </Box>
        <IconButton onClick={onClose} size="small">
          <Iconify icon="solar:close-circle-bold" />
        </IconButton>
      </DialogTitle>

      <DialogContent sx={{ p: 0, minHeight: 450, position: 'relative' }}>
        {/* Zoom SDK container - MUST exist in DOM before init(), kept in layout for proper dimensions */}
        <Box
          id={ZOOM_ROOT_ID}
          sx={{
            width: '100%',
            minHeight: 450,
            display: 'block',
          }}
        />

        {loading && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 2,
              bgcolor: 'background.default',
              zIndex: 10,
            }}
          >
            <CircularProgress size={48} />
            <Typography variant="body1" color="text.secondary">
              {t('student.connectingToMeeting')}
            </Typography>
          </Box>
        )}

        {error && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 2,
              p: 3,
              bgcolor: 'background.default',
              zIndex: 10,
            }}
          >
            <Iconify icon="solar:danger-bold" width={64} sx={{ color: 'error.main' }} />
            <Typography variant="h6" color="error" sx={{ textAlign: 'center' }}>
              {error}
            </Typography>
            <Button
              variant="contained"
              onClick={handleJoinInBrowser}
              startIcon={<Iconify icon="solar:global-bold" />}
              sx={{ mt: 2 }}
            >
              {t('student.openInBrowser')}
            </Button>
          </Box>
        )}
      </DialogContent>
    </Dialog>
  );
}

interface JoinMeetingButtonProps {
  meeting: MaterialDto;
  variant?: 'contained' | 'outlined' | 'text';
  size?: 'small' | 'medium' | 'large';
  fullWidth?: boolean;
  onOpenViewer?: (meeting: MaterialDto) => void;
}

export function JoinMeetingButton({
  meeting,
  variant = 'contained',
  size = 'medium',
  fullWidth = false,
  onOpenViewer,
}: JoinMeetingButtonProps) {
  const { t } = useTranslation();
  const [viewerOpen, setViewerOpen] = useState(false);

  const handleClick = () => {
    if (onOpenViewer) {
      onOpenViewer(meeting);
    } else {
      setViewerOpen(true);
    }
  };

  return (
    <>
      <Button
        variant={variant}
        size={size}
        fullWidth={fullWidth}
        startIcon={<Iconify icon="solar:video-bold" />}
        onClick={handleClick}
      >
        {t('student.join')}
      </Button>
      <ZoomMeetingViewer
        meeting={viewerOpen ? meeting : null}
        open={viewerOpen}
        onClose={() => setViewerOpen(false)}
      />
    </>
  );
}
