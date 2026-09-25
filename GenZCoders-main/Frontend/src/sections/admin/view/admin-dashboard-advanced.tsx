import type { ApexOptions } from 'apexcharts';

import Chart from 'react-apexcharts';
import {
  useMemo,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
  type HTMLAttributes,
} from 'react';

import Box from '@mui/material/Box';
import Tab from '@mui/material/Tab';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import Grid from '@mui/material/Grid';
import Tabs from '@mui/material/Tabs';
import Alert from '@mui/material/Alert';
import Paper from '@mui/material/Paper';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import TableRow from '@mui/material/TableRow';
import Container from '@mui/material/Container';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import Typography from '@mui/material/Typography';
import { alpha, useTheme } from '@mui/material/styles';
import TableContainer from '@mui/material/TableContainer';

import { adminApi, type DashboardData } from 'src/api';
import { DashboardContent } from 'src/layouts/dashboard';
import { useAuth } from 'src/contexts/simple-auth-context';

import { Iconify } from 'src/components/iconify';


// ----------------------------------------------------------------------

const premiumGlass = (theme: any) => ({
  background: alpha(theme.palette.background.paper, 0.8),
  backdropFilter: 'blur(20px)',
  border: `1px solid ${alpha(theme.palette.common.white, 0.2)}`,
  boxShadow: `0 8px 32px 0 ${alpha(theme.palette.common.black, 0.05)}`,
  borderRadius: 3,
});

type TabPanelProps = HTMLAttributes<HTMLDivElement> & {
  children?: ReactNode;
  index: number;
  value: number;
};

// ----------------------------------------------------------------------

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div role="tabpanel" hidden={value !== index} {...other}>
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
}

// ----------------------------------------------------------------------

export function AdminDashboardAdvanced() {
  const { user } = useAuth();
  const theme = useTheme();

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null);

  // ----------------------------------------------------------------------
  // Dashboard summary - REAL DATA
  // ----------------------------------------------------------------------

  const analytics = useMemo(() => {
    if (!dashboardData?.summary) {
      return null;
    }

    const summary = dashboardData.summary;

    return {
      totalCourses: summary.totalCourses,
      totalCourseRounds: summary.totalCourseRounds,
      totalStudents: summary.totalStudents,
      totalInstructors: summary.totalInstructors,
      totalApplications: summary.totalApplications,
      pendingApplications: summary.pendingApplications,
      totalEnrollments: summary.totalEnrollments,
      activeUsers: summary.activeUsers,
      recentRegistrations: summary.recentRegistrations,
    };
  }, [dashboardData]);

  // ----------------------------------------------------------------------
  // Recent Applications - REAL DATA
  // ----------------------------------------------------------------------

  const recentApplications = useMemo(() => {
    if (!dashboardData?.recentApplications) {
      return [];
    }

    return dashboardData.recentApplications.map(
      (app: DashboardData['recentApplications'][number]) => ({
        id: app.id.toString(),
        studentName: app.fullName,
        studentEmail: app.email ?? '',
        courseName: app.courseTitle,
        coursePrice: app.price ?? null,
        instructor: app.instructorName ?? 'Not assigned',
        appliedAt: new Date(app.applicationDate),
        status: app.statusName,
        avatar: app.fullName
          .split(' ')
          .map((name: string) => name[0])
          .join('')
          .toUpperCase()
          .slice(0, 2),
      })
    );
  }, [dashboardData]);

  // ----------------------------------------------------------------------
  // Latest Applications
  // ----------------------------------------------------------------------

  const latestApplications = useMemo(
    () =>
      [...recentApplications]
        .sort(
          (a, b) =>
            b.appliedAt.getTime() - a.appliedAt.getTime()
        )
        .slice(0, 5),
    [recentApplications]
  );

  // ----------------------------------------------------------------------
  // Fetch Dashboard
  // ----------------------------------------------------------------------

  const fetchDashboardData = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      const data = await adminApi.getDashboard();

      setDashboardData(data);
    } catch (err) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'Failed to load dashboard data';

      setError(errorMessage);

      console.error('Failed to fetch dashboard data:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDashboardData();
  }, [fetchDashboardData]);

  // ----------------------------------------------------------------------
  // Retry
  // ----------------------------------------------------------------------

  const handleRetry = useCallback(async () => {
    await fetchDashboardData();
  }, [fetchDashboardData]);

  // ----------------------------------------------------------------------
  // Stats Cards - REAL DATA
  // ----------------------------------------------------------------------

  const statsCards = [
    {
      title: 'Total Students',
      value: analytics?.totalStudents ?? 0,
      icon: 'solar:users-group-rounded-bold',
      color: theme.palette.primary.main,
      textColor: theme.palette.primary.darker,
      subtitle: `${analytics?.activeUsers ?? 0} active users`,
      trend: 'LIVE',
      bgGradient: `linear-gradient(135deg, ${alpha(
        theme.palette.primary.lighter,
        0.8
      )} 0%, ${alpha(theme.palette.primary.light, 0.9)} 100%)`,
    },
    {
      title: 'Total Instructors',
      value: analytics?.totalInstructors ?? 0,
      icon: 'solar:user-id-bold',
      color: theme.palette.info.main,
      textColor: theme.palette.info.darker,
      subtitle: 'GenZCoders instructors',
      trend: 'LIVE',
      bgGradient: `linear-gradient(135deg, ${alpha(
        theme.palette.info.lighter,
        0.8
      )} 0%, ${alpha(theme.palette.info.light, 0.9)} 100%)`,
    },
    {
      title: 'Total Courses',
      value: analytics?.totalCourses ?? 0,
      icon: 'solar:notebook-bold',
      color: theme.palette.success.main,
      textColor: theme.palette.success.darker,
      subtitle: `${analytics?.totalCourseRounds ?? 0} course rounds`,
      trend: 'LIVE',
      bgGradient: `linear-gradient(135deg, ${alpha(
        theme.palette.success.lighter,
        0.8
      )} 0%, ${alpha(theme.palette.success.light, 0.9)} 100%)`,
    },
    {
      title: 'Pending Applications',
      value: analytics?.pendingApplications ?? 0,
      icon: 'solar:clock-circle-bold',
      color: theme.palette.warning.main,
      textColor: theme.palette.warning.darker,
      subtitle: `${analytics?.totalApplications ?? 0} total applications`,
      trend: 'LIVE',
      bgGradient: `linear-gradient(135deg, ${alpha(
        theme.palette.warning.lighter,
        0.8
      )} 0%, ${alpha(theme.palette.warning.light, 0.9)} 100%)`,
    },
  ];

  // ----------------------------------------------------------------------
  // Activity Chart
  //
  // IMPORTANT:
  // We currently only have the recent applications returned by the
  // dashboard endpoint.
  //
  // We do NOT invent fake Active Users / Registration numbers.
  // ----------------------------------------------------------------------

  const activityData = useMemo(() => {
    const applications = dashboardData?.recentApplications ?? [];

    const days = Array.from({ length: 7 }, (_, index) => {
      const date = new Date();

      date.setHours(0, 0, 0, 0);
      date.setDate(date.getDate() - (6 - index));

      return date;
    });

    const categories = days.map((date) =>
      date.toLocaleDateString('en-US', {
        weekday: 'short',
      })
    );

    const applicationCounts = days.map((day) => {
      const nextDay = new Date(day);
      nextDay.setDate(nextDay.getDate() + 1);

      return applications.filter((application) => {
        const applicationDate = new Date(application.applicationDate);

        return (
          applicationDate >= day &&
          applicationDate < nextDay
        );
      }).length;
    });

    return {
      categories,
      applicationCounts,
    };
  }, [dashboardData]);

  const chartOptions: ApexOptions = {
    chart: {
      toolbar: {
        show: false,
      },
      zoom: {
        enabled: false,
      },
      fontFamily: theme.typography.fontFamily,
      background: 'transparent',
    },

    colors: [theme.palette.primary.main],

    fill: {
      type: 'gradient',
      gradient: {
        shadeIntensity: 1,
        opacityFrom: 0.7,
        opacityTo: 0.2,
        stops: [0, 90, 100],
      },
    },

    dataLabels: {
      enabled: false,
    },

    stroke: {
      curve: 'smooth',
      width: 3,
    },

    xaxis: {
      categories: activityData.categories,
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
      axisBorder: {
        show: false,
      },
      axisTicks: {
        show: false,
      },
    },

    yaxis: {
      min: 0,
      forceNiceScale: true,
      labels: {
        style: {
          colors: theme.palette.text.secondary,
        },
      },
    },

    grid: {
      strokeDashArray: 3,
      borderColor: theme.palette.divider,
      xaxis: {
        lines: {
          show: false,
        },
      },
    },

    tooltip: {
      theme: theme.palette.mode,
    },
  };

  const chartSeries = [
    {
      name: 'Applications',
      data: activityData.applicationCounts,
    },
  ];

  // ----------------------------------------------------------------------
  // Course information available from current dashboard data
  //
  // We don't invent ratings, revenue, completion, etc.
  // ----------------------------------------------------------------------

  const courseSummary = useMemo(() => {
    const applications = dashboardData?.recentApplications ?? [];

    const grouped = new Map<
      number,
      {
        courseId: number;
        title: string;
        applications: number;
        instructors: Set<string>;
        prices: number[];
      }
    >();

    applications.forEach((application) => {
      const existing = grouped.get(application.courseId);

      if (existing) {
        existing.applications += 1;

        if (application.instructorName) {
          existing.instructors.add(application.instructorName);
        }

        if (typeof application.price === 'number') {
          existing.prices.push(application.price);
        }
      } else {
        grouped.set(application.courseId, {
          courseId: application.courseId,
          title: application.courseTitle,
          applications: 1,
          instructors: new Set(
            application.instructorName
              ? [application.instructorName]
              : []
          ),
          prices:
            typeof application.price === 'number'
              ? [application.price]
              : [],
        });
      }
    });

    return Array.from(grouped.values())
      .map((course) => ({
        ...course,
        instructor:
          Array.from(course.instructors).join(', ') ||
          'Not assigned',
        price:
          course.prices.length > 0
            ? course.prices[0]
            : null,
      }))
      .sort((a, b) => b.applications - a.applications);
  }, [dashboardData]);

  // ----------------------------------------------------------------------
  // System information
  //
  // These values are DB-backed, not fake monitoring numbers.
  // ----------------------------------------------------------------------

  const systemInfo = useMemo(
  () => ({
    totalUsers: analytics?.activeUsers ?? 0,
    totalCourses: analytics?.totalCourses ?? 0,
    totalApplications: analytics?.totalApplications ?? 0,
    pendingApplications: analytics?.pendingApplications ?? 0,
  }),
  [analytics]
);

  // ----------------------------------------------------------------------
  // Render
  // ----------------------------------------------------------------------

  return (
    <DashboardContent>
      <Container maxWidth="xl">

        {/* Error */}
        {error && (
          <Box sx={{ mb: 3 }}>
            <Alert
              severity="error"
              action={
                <Button
                  color="inherit"
                  size="small"
                  onClick={handleRetry}
                >
                  Retry
                </Button>
              }
            >
              {error}
            </Alert>
          </Box>
        )}

        {/* Loading */}
        {isLoading && (
          <Box sx={{ mb: 3 }}>
            <Card sx={{ p: 2 }}>
              <Typography
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  fontWeight: 700,
                }}
              >
                Loading dashboard...
              </Typography>
            </Card>
          </Box>
        )}

        {/* ------------------------------------------------------------ */}
        {/* Header */}
        {/* ------------------------------------------------------------ */}

        <Box
          sx={{
            mb: 6,
            p: 4,
            borderRadius: 3,
            position: 'relative',
            overflow: 'hidden',
            background: `linear-gradient(
              135deg,
              ${theme.palette.secondary.dark} 0%,
              ${theme.palette.primary.main} 100%
            )`,
            color: 'white',
            boxShadow: theme.shadows[8],
          }}
        >
          <Box
            sx={{
              position: 'relative',
              zIndex: 1,
              display: 'flex',
              alignItems: 'center',
              gap: 3,
            }}
          >
            <Avatar
              alt={user?.name}
              sx={{
                width: 72,
                height: 72,
                bgcolor: 'white',
                color: 'primary.main',
                fontSize: '2rem',
                fontWeight: 'bold',
                boxShadow: '0 8px 24px rgba(0,0,0,0.2)',
              }}
            >
              {user?.name?.charAt(0) || 'A'}
            </Avatar>

            <Box>
              <Typography
                variant="h3"
                sx={{
                  fontWeight: 800,
                  mb: 1,
                }}
              >
                Admin Dashboard
              </Typography>

              <Typography
                variant="body1"
                sx={{
                  opacity: 0.9,
                }}
              >
                Welcome back, {user?.name}! Here&apos;s your platform
                overview
              </Typography>
            </Box>
          </Box>

          <Box
            sx={{
              position: 'absolute',
              top: -60,
              right: -60,
              width: 300,
              height: 300,
              borderRadius: '50%',
              background:
                'radial-gradient(circle, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0) 70%)',
            }}
          />
        </Box>

        {/* ------------------------------------------------------------ */}
        {/* Key Metrics */}
        {/* ------------------------------------------------------------ */}

        <Grid
          container
          spacing={3}
          sx={{ mb: 6 }}
        >
          {statsCards.map((card, index) => (
            <Grid
              key={index}
              size={{
                xs: 12,
                sm: 6,
                md: 3,
              }}
            >
              <Card
                sx={{
                  height: '100%',
                  ...premiumGlass(theme),
                  background: alpha(
                    theme.palette.background.paper,
                    0.5
                  ),
                  color: 'text.primary',
                  position: 'relative',
                  overflow: 'hidden',
                  p: 3,
                  transition:
                    'all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275)',
                  cursor: 'default',

                  '&:hover': {
                    transform:
                      'translateY(-8px) scale(1.02)',
                    boxShadow: `0 20px 40px -12px ${alpha(
                      card.color,
                      0.3
                    )}`,
                    borderColor: alpha(
                      card.color,
                      0.5
                    ),

                    '& .decorative-blob': {
                      transform: 'scale(1.5)',
                      opacity: 0.4,
                    },

                    '& .icon-box': {
                      transform:
                        'rotate(5deg) scale(1.1)',
                    },
                  },
                }}
              >
                <Box
                  className="decorative-blob"
                  sx={{
                    position: 'absolute',
                    top: -30,
                    right: -30,
                    width: 120,
                    height: 120,
                    borderRadius: '50%',
                    background: `radial-gradient(
                      circle,
                      ${alpha(card.color, 0.3)} 0%,
                      transparent 70%
                    )`,
                    transition: 'all 0.6s ease',
                    zIndex: 0,
                  }}
                />

                <Box
                  sx={{
                    display: 'flex',
                    justifyContent:
                      'space-between',
                    alignItems: 'flex-start',
                    mb: 4,
                    position: 'relative',
                    zIndex: 1,
                  }}
                >
                  <Box
                    className="icon-box"
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      width: 52,
                      height: 52,
                      borderRadius: '16px',
                      background: `linear-gradient(
                        135deg,
                        ${alpha(card.color, 0.2)} 0%,
                        ${alpha(card.color, 0.05)} 100%
                      )`,
                      color: card.color,
                      boxShadow: `inset 0px 2px 4px 0px ${alpha(
                        theme.palette.common.white,
                        0.2
                      )}`,
                      border: `1px solid ${alpha(
                        card.color,
                        0.15
                      )}`,
                      transition: 'all 0.3s ease',
                    }}
                  >
                    <Iconify
                      icon={card.icon}
                      width={28}
                    />
                  </Box>

                  <Chip
                    label={card.trend}
                    size="small"
                    sx={{
                      bgcolor: alpha(
                        card.color,
                        0.1
                      ),
                      color: card.color,
                      fontWeight: 800,
                      fontSize: '0.75rem',
                      borderRadius: 1.5,
                      border: `1px solid ${alpha(
                        card.color,
                        0.2
                      )}`,
                      backdropFilter: 'blur(4px)',
                      px: 0.5,
                    }}
                  />
                </Box>

                <Box
                  sx={{
                    position: 'relative',
                    zIndex: 1,
                  }}
                >
                  <Typography
                    variant="h3"
                    sx={{
                      fontWeight: 900,
                      mb: 0.5,
                      letterSpacing: '-0.5px',
                    }}
                  >
                    {card.value}
                  </Typography>

                  <Typography
                    variant="subtitle2"
                    sx={{
                      color: 'text.secondary',
                      fontWeight: 600,
                      letterSpacing: '0.2px',
                    }}
                  >
                    {card.title}
                  </Typography>

                  <Typography
                    variant="caption"
                    sx={{
                      display: 'block',
                      mt: 1,
                      color: 'text.secondary',
                      opacity: 0.8,
                    }}
                  >
                    {card.subtitle}
                  </Typography>
                </Box>
              </Card>
            </Grid>
          ))}
        </Grid>

        {/* ------------------------------------------------------------ */}
        {/* Activity Chart */}
        {/* ------------------------------------------------------------ */}

        <Card
          sx={{
            mb: 6,
            p: 3,
            boxShadow: theme.shadows[2],
          }}
        >
          <Stack
            direction="row"
            alignItems="center"
            justifyContent="space-between"
            sx={{ mb: 3 }}
          >
            <Box>
              <Typography
                variant="h6"
                sx={{ fontWeight: 700 }}
              >
                Application Activity
              </Typography>

              <Typography
                variant="body2"
                color="text.secondary"
              >
                Based on real application data
              </Typography>
            </Box>
          </Stack>

          <Box
            sx={{
              height: 350,
              width: '100%',
            }}
          >
            <Chart
              options={chartOptions}
              series={chartSeries}
              type="area"
              height={350}
            />
          </Box>
        </Card>

        {/* ------------------------------------------------------------ */}
        {/* Main Tabs */}
        {/* ------------------------------------------------------------ */}

        <Card
          sx={{
            mb: 6,
            boxShadow: theme.shadows[3],
          }}
        >
          <Tabs
            value={tabValue}
            onChange={(event, newValue) =>
              setTabValue(newValue)
            }
            sx={{
              borderBottom: 1,
              borderColor: 'divider',
              px: 3,

              '& .MuiTab-root': {
                textTransform: 'none',
                fontSize: '1rem',
                fontWeight: 600,
                color: 'text.secondary',

                '&.Mui-selected': {
                  color: 'primary.main',
                },
              },
            }}
          >
            <Tab
              label="Recent Applications"
              icon={
                <Iconify icon="solar:pen-bold-duotone" />
              }
              iconPosition="start"
            />

            <Tab
              label="Top Courses"
              icon={
                <Iconify icon="solar:star-bold-duotone" />
              }
              iconPosition="start"
            />

            <Tab
              label="System Health"
              icon={
                <Iconify icon="solar:server-square-bold-duotone" />
              }
              iconPosition="start"
            />
          </Tabs>

          {/* ---------------------------------------------------------- */}
          {/* Applications */}
          {/* ---------------------------------------------------------- */}

          <TabPanel
            value={tabValue}
            index={0}
          >
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow
                    sx={{
                      bgcolor:
                        'background.neutral',
                    }}
                  >
                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Student
                    </TableCell>

                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Course
                    </TableCell>

                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Instructor
                    </TableCell>

                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Price
                    </TableCell>

                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Status
                    </TableCell>

                    <TableCell
                      sx={{ fontWeight: 700 }}
                    >
                      Applied Date
                    </TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {latestApplications.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{ py: 2 }}
                        >
                          No recent applications.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    latestApplications.map(
                      (app) => (
                        <TableRow
                          key={app.id}
                          sx={{
                            '&:hover': {
                              bgcolor: alpha(
                                theme.palette
                                  .primary
                                  .lighter,
                                0.2
                              ),
                            },
                          }}
                        >
                          {/* Student */}
                          <TableCell>
                            <Box
                              sx={{
                                display: 'flex',
                                alignItems:
                                  'center',
                                gap: 2,
                              }}
                            >
                              <Avatar
                                sx={{
                                  bgcolor:
                                    'primary.lighter',
                                  color:
                                    'primary.dark',
                                  width: 40,
                                  height: 40,
                                  fontWeight:
                                    'bold',
                                }}
                              >
                                {app.avatar}
                              </Avatar>

                              <Box>
                                <Typography
                                  variant="subtitle2"
                                  sx={{
                                    fontWeight: 600,
                                  }}
                                >
                                  {
                                    app.studentName
                                  }
                                </Typography>

                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                >
                                  {
                                    app.studentEmail
                                  }
                                </Typography>
                              </Box>
                            </Box>
                          </TableCell>

                          {/* Course */}
                          <TableCell>
                            <Typography
                              variant="body2"
                              sx={{
                                fontWeight: 600,
                              }}
                            >
                              {app.courseName}
                            </Typography>
                          </TableCell>

                          {/* Instructor */}
                          <TableCell>
                            <Typography
                              variant="body2"
                              color="text.secondary"
                            >
                              {app.instructor}
                            </Typography>
                          </TableCell>

                          {/* Price */}
                          <TableCell>
                            <Typography
                              variant="subtitle2"
                              sx={{
                                fontWeight: 700,
                                color:
                                  app.coursePrice !==
                                  null
                                    ? 'success.main'
                                    : 'text.secondary',
                              }}
                            >
                              {app.coursePrice !==
                              null
                                ? `$${app.coursePrice.toFixed(
                                    2
                                  )}`
                                : 'Not set'}
                            </Typography>
                          </TableCell>

                          {/* Status */}
                          <TableCell>
                            <Chip
                              label={app.status}
                              size="small"
                              color={
                                app.status
                                  .toLowerCase()
                                  .includes(
                                    'pending'
                                  )
                                  ? 'warning'
                                  : app.status
                                      .toLowerCase()
                                      .includes(
                                        'accepted'
                                      ) ||
                                    app.status
                                      .toLowerCase()
                                      .includes(
                                        'active'
                                      )
                                    ? 'success'
                                    : 'default'
                              }
                              variant="outlined"
                            />
                          </TableCell>

                          {/* Date */}
                          <TableCell>
                            {app.appliedAt.toLocaleDateString()}
                          </TableCell>
                        </TableRow>
                      )
                    )
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </TabPanel>

          {/* ---------------------------------------------------------- */}
          {/* Top Courses */}
          {/* ---------------------------------------------------------- */}

          <TabPanel
            value={tabValue}
            index={1}
          >
            {courseSummary.length === 0 ? (
              <Box
                sx={{
                  p: 5,
                  textAlign: 'center',
                }}
              >
                <Iconify
                  icon="solar:notebook-bold-duotone"
                  width={50}
                  color="text.disabled"
                />

                <Typography
                  variant="h6"
                  sx={{ mt: 2 }}
                >
                  No course application data
                </Typography>

                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mt: 1 }}
                >
                  Real course statistics will appear
                  here when application data is
                  available.
                </Typography>
              </Box>
            ) : (
              <Grid
                container
                spacing={3}
              >
                {courseSummary.map(
                  (course, index) => (
                    <Grid
                      key={course.courseId}
                      size={{
                        xs: 12,
                        md: 4,
                      }}
                    >
                      <Paper
                        sx={{
                          p: 3,
                          background: alpha(
                            theme.palette
                              .background
                              .neutral,
                            0.5
                          ),
                          border: '1px solid',
                          borderColor:
                            'divider',
                          borderRadius: 2,
                          transition:
                            'all 0.3s ease',

                          '&:hover': {
                            boxShadow:
                              theme.shadows[10],
                            transform:
                              'translateY(-4px)',
                            bgcolor:
                              'background.paper',
                          },
                        }}
                      >
                        <Box
                          sx={{
                            display: 'flex',
                            alignItems:
                              'flex-start',
                            justifyContent:
                              'space-between',
                            gap: 2,
                            mb: 2,
                          }}
                        >
                          <Typography
                            variant="h6"
                            sx={{
                              fontWeight: 800,
                            }}
                          >
                            {course.title}
                          </Typography>

                          <Chip
                            label={`#${index + 1}`}
                            size="small"
                            color={
                              index === 0
                                ? 'success'
                                : 'default'
                            }
                            variant={
                              index === 0
                                ? 'filled'
                                : 'outlined'
                            }
                            sx={{
                              fontWeight: 900,
                            }}
                          />
                        </Box>

                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{
                            display: 'flex',
                            alignItems:
                              'center',
                            gap: 0.5,
                            mb: 3,
                          }}
                        >
                          <Iconify
                            icon="solar:user-circle-bold"
                            width={14}
                          />

                          {course.instructor}
                        </Typography>

                        <Box
                          sx={{
                            display: 'grid',
                            gridTemplateColumns:
                              '1fr 1fr',
                            gap: 2,
                          }}
                        >
                          <Box>
                            <Typography
                              variant="h5"
                              color="primary.main"
                              sx={{
                                fontWeight: 700,
                              }}
                            >
                              {
                                course.applications
                              }
                            </Typography>

                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Applications
                            </Typography>
                          </Box>

                          <Box>
                            <Typography
                              variant="h5"
                              color="success.main"
                              sx={{
                                fontWeight: 700,
                              }}
                            >
                              {course.price !==
                              null
                                ? `$${course.price.toFixed(
                                    2
                                  )}`
                                : 'N/A'}
                            </Typography>

                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Course Price
                            </Typography>
                          </Box>
                        </Box>
                      </Paper>
                    </Grid>
                  )
                )}
              </Grid>
            )}
          </TabPanel>

          {/* ---------------------------------------------------------- */}
          {/* System Health */}
          {/* ---------------------------------------------------------- */}

          <TabPanel
            value={tabValue}
            index={2}
          >
            <Card
              sx={{
                mb: 3,
                p: 3,
                borderRadius: 2,
                border: '1px solid',
                borderColor: alpha(
                  theme.palette.success.main,
                  0.22
                ),
                bgcolor: alpha(
                  theme.palette.success.lighter,
                  0.22
                ),
              }}
            >
              <Stack
                direction={{
                  xs: 'column',
                  md: 'row',
                }}
                spacing={2}
                alignItems={{
                  md: 'center',
                }}
                justifyContent="space-between"
              >
                <Box
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1.5,
                  }}
                >
                  <Box
                    sx={{
                      width: 44,
                      height: 44,
                      borderRadius: 2,
                      display: 'flex',
                      alignItems:
                        'center',
                      justifyContent:
                        'center',
                      bgcolor: alpha(
                        theme.palette.success
                          .main,
                        0.14
                      ),
                      color: 'success.main',
                    }}
                  >
                    <Iconify
                      icon="solar:database-bold-duotone"
                      width={24}
                    />
                  </Box>

                  <Box>
                    <Typography
                      variant="h6"
                      sx={{
                        fontWeight: 900,
                      }}
                    >
                      Dashboard Data Connected
                    </Typography>

                    <Typography
                      variant="body2"
                      color="text.secondary"
                    >
                      These values are loaded from
                      the GenZCoders database.
                    </Typography>
                  </Box>
                </Box>

                <Chip
                  label="LIVE DATA"
                  size="small"
                  color="success"
                  variant="outlined"
                  sx={{
                    fontWeight: 800,
                  }}
                />
              </Stack>
            </Card>

            <Grid
              container
              spacing={3}
            >
              {/* Active Users */}
              <Grid
                size={{
                  xs: 12,
                  sm: 6,
                  md: 3,
                }}
              >
                <Paper
                  sx={{
                    p: 3,
                    textAlign: 'center',
                    background: alpha(
                      theme.palette.info.lighter,
                      0.25
                    ),
                    border: '1px solid',
                    borderColor: alpha(
                      theme.palette.info.main,
                      0.25
                    ),
                    borderRadius: 2,
                  }}
                >
                  <Iconify
                    icon="solar:users-group-rounded-bold-duotone"
                    width={40}
                    color="info.main"
                  />

                  <Typography
                    variant="h4"
                    sx={{
                      fontWeight: 700,
                      mt: 2,
                    }}
                  >
                    {systemInfo.totalUsers}
                  </Typography>

                  <Typography
                    variant="body2"
                    color="text.secondary"
                  >
                    Active Users
                  </Typography>
                </Paper>
              </Grid>

              {/* Courses */}
              <Grid
                size={{
                  xs: 12,
                  sm: 6,
                  md: 3,
                }}
              >
                <Paper
                  sx={{
                    p: 3,
                    textAlign: 'center',
                    background: alpha(
                      theme.palette.success.lighter,
                      0.25
                    ),
                    border: '1px solid',
                    borderColor: alpha(
                      theme.palette.success.main,
                      0.25
                    ),
                    borderRadius: 2,
                  }}
                >
                  <Iconify
                    icon="solar:notebook-bold-duotone"
                    width={40}
                    color="success.main"
                  />

                  <Typography
                    variant="h4"
                    sx={{
                      fontWeight: 700,
                      mt: 2,
                    }}
                  >
                    {systemInfo.totalCourses}
                  </Typography>

                  <Typography
                    variant="body2"
                    color="text.secondary"
                  >
                    Total Courses
                  </Typography>
                </Paper>
              </Grid>

              {/* Applications */}
              <Grid
                size={{
                  xs: 12,
                  sm: 6,
                  md: 3,
                }}
              >
                <Paper
                  sx={{
                    p: 3,
                    textAlign: 'center',
                    background: alpha(
                      theme.palette.primary.lighter,
                      0.25
                    ),
                    border: '1px solid',
                    borderColor: alpha(
                      theme.palette.primary.main,
                      0.25
                    ),
                    borderRadius: 2,
                  }}
                >
                  <Iconify
                    icon="solar:document-text-bold-duotone"
                    width={40}
                    color="primary.main"
                  />

                  <Typography
                    variant="h4"
                    sx={{
                      fontWeight: 700,
                      mt: 2,
                    }}
                  >
                    {systemInfo.totalApplications}
                  </Typography>

                  <Typography
                    variant="body2"
                    color="text.secondary"
                  >
                    Total Applications
                  </Typography>
                </Paper>
              </Grid>

              {/* Pending */}
              <Grid
                size={{
                  xs: 12,
                  sm: 6,
                  md: 3,
                }}
              >
                <Paper
                  sx={{
                    p: 3,
                    textAlign: 'center',
                    background: alpha(
                      theme.palette.warning.lighter,
                      0.25
                    ),
                    border: '1px solid',
                    borderColor: alpha(
                      theme.palette.warning.main,
                      0.25
                    ),
                    borderRadius: 2,
                  }}
                >
                  <Iconify
                    icon="solar:clock-circle-bold-duotone"
                    width={40}
                    color="warning.main"
                  />

                  <Typography
                    variant="h4"
                    sx={{
                      fontWeight: 700,
                      mt: 2,
                    }}
                  >
                    {
                      systemInfo.pendingApplications
                    }
                  </Typography>

                  <Typography
                    variant="body2"
                    color="text.secondary"
                  >
                    Pending Applications
                  </Typography>
                </Paper>
              </Grid>
            </Grid>
          </TabPanel>
        </Card>

        {/* ------------------------------------------------------------ */}
        {/* Quick Actions */}
        {/* ------------------------------------------------------------ */}

        <Grid
          container
          spacing={2}
        >
          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 3,
            }}
          >
            <Button
              variant="contained"
              fullWidth
              size="large"
              startIcon={
                <Iconify icon="solar:pen-bold" />
              }
              sx={{
                py: 2,
                borderRadius: 2,
                boxShadow: theme.shadows[8],
              }}
              href="/admin/applications"
            >
              Review Applications
            </Button>
          </Grid>

          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 3,
            }}
          >
            <Button
              variant="outlined"
              fullWidth
              size="large"
              startIcon={
                <Iconify icon="solar:users-group-rounded-bold" />
              }
              sx={{
                py: 2,
                borderRadius: 2,
                borderWidth: 2,
              }}
              href="/admin/users"
            >
              Manage Users
            </Button>
          </Grid>

          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 3,
            }}
          >
            <Button
              variant="outlined"
              fullWidth
              size="large"
              startIcon={
                <Iconify icon="solar:notebook-bold" />
              }
              sx={{
                py: 2,
                borderRadius: 2,
                borderWidth: 2,
              }}
              href="/admin/courses"
            >
              Course Management
            </Button>
          </Grid>

          <Grid
            size={{
              xs: 12,
              sm: 6,
              md: 3,
            }}
          >
            <Button
              variant="outlined"
              fullWidth
              size="large"
              startIcon={
                <Iconify icon="solar:chart-bold" />
              }
              sx={{
                py: 2,
                borderRadius: 2,
                borderWidth: 2,
              }}
              href="/admin/reports"
            >
              Reports
            </Button>
          </Grid>
        </Grid>
      </Container>
    </DashboardContent>
  );
}