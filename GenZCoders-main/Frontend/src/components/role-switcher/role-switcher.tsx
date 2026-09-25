
import type { UserRole } from 'src/types/user';

import { useState } from 'react';

import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import Typography from '@mui/material/Typography';
import DialogTitle from '@mui/material/DialogTitle';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';

import { useAuth } from 'src/contexts/simple-auth-context';

import { Iconify } from 'src/components/iconify';

// ----------------------------------------------------------------------

const roleConfig: Record<
  UserRole,
  {
    label: string;
    color: 'error' | 'success' | 'warning' | 'info' | 'secondary';
    description: string;
    email?: string;
  }
> = {
  admin: {
    label: 'Admin',
    color: 'error',
    description: 'Full system access - can manage users, courses, and view reports',
    email: 'admin@school.com',
  },

  engineer: {
    label: 'Engineer',
    color: 'success',
    description: 'Technical support and system maintenance access',
    email: 'engineer@school.com',
  },

  instructor: {
    label: 'Instructor',
    color: 'warning',
    description: 'Can create and manage courses, view students, handle assignments',
    email: 'instructor@school.com',
  },

  student: {
    label: 'Student',
    color: 'info',
    description: 'Can enroll in courses, view assignments, and track grades',
    email: 'student@school.com',
  },

  board: {
    label: 'Board',
    color: 'secondary',
    description: 'Board access for the assigned business entity',
  },
};

// ----------------------------------------------------------------------

export function RoleSwitcher() {
  const { user, login, logout } = useAuth();

  const [open, setOpen] = useState(false);
  const [switching, setSwitching] = useState(false);

  const handleSwitchRole = async (role: UserRole) => {
    // Board does not have a test account configured here.
    // Do not attempt to login using fake credentials.
    if (!roleConfig[role].email) {
      console.warn(`No test account configured for role: ${role}`);
      return;
    }

    setSwitching(true);

    try {
      await logout();

      await login(roleConfig[role].email, 'password123');

      setOpen(false);
    } catch (error) {
      console.error('Failed to switch role:', error);
    } finally {
      setSwitching(false);
    }
  };

  const currentRole: UserRole = user?.role || 'student';

  const currentRoleConfig = roleConfig[currentRole];

  return (
    <>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Typography variant="body2" color="text.secondary">
          Testing as:
        </Typography>

        <Chip
          label={currentRoleConfig.label}
          color={currentRoleConfig.color}
          size="small"
          icon={<Iconify icon="solar:pen-bold" width={16} />}
          onClick={() => setOpen(true)}
          sx={{ cursor: 'pointer' }}
        />

        <Button
          size="small"
          variant="outlined"
          onClick={() => setOpen(true)}
          startIcon={<Iconify icon="solar:restart-bold" />}
        >
          Switch Role
        </Button>
      </Box>

      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Iconify icon="solar:pen-bold" />
            Switch User Role
          </Box>
        </DialogTitle>

        <DialogContent>
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ mb: 3 }}
          >
            Choose a role to test different functionalities of the course
            management system.
          </Typography>

          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {(
              Object.entries(roleConfig) as [
                UserRole,
                (typeof roleConfig)[UserRole]
              ][]
            ).map(([role, config]) => (
              <Box
                key={role}
                onClick={() => {
                  if (!switching && config.email) {
                    handleSwitchRole(role);
                  }
                }}
                sx={{
                  p: 2,
                  border: 1,
                  borderColor:
                    currentRole === role ? 'primary.main' : 'divider',
                  borderRadius: 1,
                  cursor:
                    switching || !config.email ? 'not-allowed' : 'pointer',
                  opacity: switching || !config.email ? 0.6 : 1,
                  bgcolor:
                    currentRole === role
                      ? 'primary.lighter'
                      : 'transparent',
                  '&:hover': {
                    bgcolor:
                      currentRole === role
                        ? 'primary.lighter'
                        : config.email
                          ? 'action.hover'
                          : 'transparent',
                  },
                }}
              >
                <Box
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1,
                    mb: 1,
                  }}
                >
                  <Chip
                    label={config.label}
                    color={config.color}
                    size="small"
                  />

                  {currentRole === role && (
                    <Chip
                      label="Current"
                      size="small"
                      variant="outlined"
                      color="primary"
                    />
                  )}
                </Box>

                <Typography variant="body2" color="text.secondary">
                  {config.description}
                </Typography>

                {config.email ? (
                  <Typography
                    variant="caption"
                    color="text.disabled"
                    sx={{
                      mt: 0.5,
                      display: 'block',
                    }}
                  >
                    Email: {config.email}
                  </Typography>
                ) : (
                  <Typography
                    variant="caption"
                    color="text.disabled"
                    sx={{
                      mt: 0.5,
                      display: 'block',
                    }}
                  >
                    No test account configured
                  </Typography>
                )}
              </Box>
            ))}
          </Box>
        </DialogContent>

        <DialogActions>
          <Button onClick={() => setOpen(false)}>
            Cancel
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

