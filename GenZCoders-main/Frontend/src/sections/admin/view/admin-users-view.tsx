import type { User, UserRole } from 'src/types/user';

import {
  useMemo,
  useState,
  useEffect,
  useCallback,
} from 'react';

import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import Grid from '@mui/material/Grid';
import Alert from '@mui/material/Alert';
import Table from '@mui/material/Table';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import TableRow from '@mui/material/TableRow';
import Container from '@mui/material/Container';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import CardContent from '@mui/material/CardContent';
import { alpha, useTheme } from '@mui/material/styles';
import TableContainer from '@mui/material/TableContainer';

import { adminApi } from 'src/api';
import { DashboardContent } from 'src/layouts/dashboard';

import { Iconify } from 'src/components/iconify';

// ----------------------------------------------------------------------

const getRoleFromRoleName = (roleName?: string): UserRole => {
  const role = roleName?.trim().toLowerCase();

  switch (role) {
    case 'admin':
      return 'admin';

    case 'engineer':
      return 'engineer';

    case 'co-instructor':
    case 'instructor':
      return 'instructor';

    case 'board':
      return 'board';

    case 'student':
      return 'student';

    default:
      return 'student';
  }
};

// Convert frontend role to the exact role name stored in DB.
const getRoleNameForApi = (role: UserRole) => {
  switch (role) {
    case 'student':
      return 'Student';

    case 'instructor':
      return 'co-Instructor';

    case 'engineer':
      return 'Engineer';

    case 'board':
      return 'Board';

    default:
      return 'Student';
  }
};

// ----------------------------------------------------------------------

const getRoleColor = (role: UserRole) => {
  switch (role) {
    case 'admin':
      return 'error';

    case 'engineer':
      return 'secondary';

    case 'board':
      return 'primary';

    case 'instructor':
      return 'warning';

    case 'student':
    default:
      return 'info';
  }
};

// ----------------------------------------------------------------------

export function AdminUsersView() {
  const theme = useTheme();

  const [users, setUsers] = useState<User[]>([]);
  const [openDialog, setOpenDialog] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [newUser, setNewUser] = useState({
    name: '',
    email: '',
    password: '',
    nationalId: '',
    phone: '',
    role: 'student' as UserRole,
    isActive: true,
  });

  const [query, setQuery] = useState('');
  const [editingUserId, setEditingUserId] = useState<string | null>(null);

  // ----------------------------------------------------------------------
  // Load users from DB
  // ----------------------------------------------------------------------

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await adminApi.getAllUsers();

      const mappedUsers: User[] = data.map((user) => ({
        id: String(user.id),
        name: user.fullNameEn || user.fullNameAr || 'Unknown User',
        email: user.email || '',
        phone: user.phone,
        role: getRoleFromRoleName(user.roleName),
        roleName: user.roleName,
        isActive: user.isActive,
        createdAt: new Date(user.createdAt),
        updatedAt: user.lastLoginAt
          ? new Date(user.lastLoginAt)
          : new Date(user.createdAt),
        avatar: undefined,
      }));

      setUsers(mappedUsers);
    } catch (err) {
      console.error('Failed to load users:', err);
      setError('Failed to load users from the server.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  // ----------------------------------------------------------------------
  // Filter
  // ----------------------------------------------------------------------

  const filteredUsers = useMemo(() => {
    const q = query.trim().toLowerCase();

    if (!q) {
      return users;
    }

    return users.filter((user) => {
      const name = user.name?.toLowerCase() ?? '';
      const email = user.email?.toLowerCase() ?? '';
      const role =
        user.roleName?.toLowerCase() ??
        user.role?.toLowerCase() ??
        '';

      return (
        name.includes(q) ||
        email.includes(q) ||
        role.includes(q)
      );
    });
  }, [query, users]);

  // ----------------------------------------------------------------------
  // Stats
  // ----------------------------------------------------------------------

  const totalUsers = users.length;

  const activeUsers = users.filter(
    (user) => user.isActive
  ).length;

  const instructors = users.filter((user) => {
    const roleName = user.roleName?.toLowerCase();

    return (
      roleName === 'co-instructor' ||
      roleName === 'instructor' ||
      user.role === 'instructor'
    );
  }).length;

  const students = users.filter((user) => {
    const roleName = user.roleName?.toLowerCase();

    return (
      roleName === 'student' ||
      user.role === 'student'
    );
  }).length;

  // ----------------------------------------------------------------------
  // Dialog
  // ----------------------------------------------------------------------

  const openCreateDialog = useCallback(() => {
    setEditingUserId(null);

    setNewUser({
      name: '',
      email: '',
      password: '',
      nationalId: '',
      phone: '',
      role: 'student',
      isActive: true,
    });

    setError(null);
    setOpenDialog(true);
  }, []);

  const openEditDialog = useCallback((user: User) => {
    setEditingUserId(String(user.id));

    setNewUser({
      name: user.name ?? '',
      email: user.email ?? '',
      password: '',
      nationalId: '',
      phone: user.phone ?? '',
      role: user.role ?? 'student',
      isActive: user.isActive,
    });

    setError(null);
    setOpenDialog(true);
  }, []);

  const handleCloseDialog = useCallback(() => {
    if (isSaving) {
      return;
    }

    setOpenDialog(false);
    setEditingUserId(null);

    setNewUser({
      name: '',
      email: '',
      password: '',
      nationalId: '',
      phone: '',
      role: 'student',
      isActive: true,
    });
  }, [isSaving]);

  // ----------------------------------------------------------------------
  // Delete / Deactivate
  // ----------------------------------------------------------------------

  const handleDeleteUser = useCallback(
    async (id: string) => {
      const confirmed = window.confirm(
        'Are you sure you want to deactivate this user?'
      );

      if (!confirmed) {
        return;
      }

      setError(null);

      try {
        await adminApi.deleteUser(Number(id));

        await loadUsers();
      } catch (err) {
        console.error('Failed to deactivate user:', err);
        setError('Failed to deactivate user.');
      }
    },
    [loadUsers]
  );

  // ----------------------------------------------------------------------
  // Activate
  // ----------------------------------------------------------------------

  const handleActivateUser = useCallback(
    async (id: string) => {
      setError(null);

      try {
        await adminApi.activateUser(Number(id));

        await loadUsers();
      } catch (err) {
        console.error('Failed to activate user:', err);
        setError('Failed to activate user.');
      }
    },
    [loadUsers]
  );

  // ----------------------------------------------------------------------
  // Save
  // ----------------------------------------------------------------------

  const handleSaveUser = useCallback(async () => {
    const name = newUser.name.trim();
    const email = newUser.email.trim();
    const password = newUser.password.trim();
    const nationalId = newUser.nationalId.trim();
    const phone = newUser.phone.trim();

    if (!name || !email) {
      setError('Please provide name and email.');
      return;
    }

    if (!editingUserId && !password) {
      setError('Password is required when creating a user.');
      return;
    }

    if (!editingUserId && !nationalId) {
      setError('National ID is required when creating a user.');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const roleName = getRoleNameForApi(newUser.role);

      if (editingUserId) {
        await adminApi.updateUser(Number(editingUserId), {
          email,
          password: password || undefined,
          fullNameEn: name,
          fullNameAr: undefined,
          phone: phone || undefined,
          roleName,
          isActive: newUser.isActive,
        });
      } else {
        await adminApi.createUser({
          email,
          password,
          nationalId,
          fullNameEn: name,
          fullNameAr: undefined,
          phone: phone || undefined,
          roleName,
        });
      }

      handleCloseDialog();
      await loadUsers();
    } catch (err) {
      console.error('Failed to save user:', err);

      const message =
        err instanceof Error
          ? err.message
          : 'Failed to save user.';

      setError(message);
    } finally {
      setIsSaving(false);
    }
  }, [
    editingUserId,
    handleCloseDialog,
    loadUsers,
    newUser.email,
    newUser.isActive,
    newUser.name,
    newUser.nationalId,
    newUser.password,
    newUser.phone,
    newUser.role,
  ]);

  // ----------------------------------------------------------------------
  // Retry
  // ----------------------------------------------------------------------

  const handleRetry = useCallback(() => {
    loadUsers();
  }, [loadUsers]);

  // ----------------------------------------------------------------------
  // Role Distribution
  // ----------------------------------------------------------------------

  const roleDistribution = useMemo(() => {
    const admins = users.filter(
      (user) =>
        user.roleName?.toLowerCase() === 'admin'
    ).length;

    const engineers = users.filter(
      (user) =>
        user.roleName?.toLowerCase() === 'engineer' ||
        user.role === 'engineer'
    ).length;

    const boards = users.filter(
      (user) =>
        user.roleName?.toLowerCase() === 'board' ||
        user.role === 'board'
    ).length;

    if (totalUsers === 0) {
      return [
        {
          label: 'Students',
          value: 0,
          count: 0,
          color: 'info',
        },
        {
          label: 'Instructors',
          value: 0,
          count: 0,
          color: 'warning',
        },
        {
          label: 'Engineers',
          value: 0,
          count: 0,
          color: 'secondary',
        },
        {
          label: 'Board',
          value: 0,
          count: 0,
          color: 'primary',
        },
      ];
    }

    return [
      {
        label: 'Students',
        value: Math.round(
          (students / totalUsers) * 100
        ),
        count: students,
        color: 'info',
      },
      {
        label: 'Instructors',
        value: Math.round(
          (instructors / totalUsers) * 100
        ),
        count: instructors,
        color: 'warning',
      },
      {
        label: 'Engineers',
        value: Math.round(
          (engineers / totalUsers) * 100
        ),
        count: engineers,
        color: 'secondary',
      },
      {
        label: 'Board',
        value: Math.round(
          (boards / totalUsers) * 100
        ),
        count: boards,
        color: 'primary',
      },
      ...(admins > 0
        ? [
            {
              label: 'Admins',
              value: Math.round(
                (admins / totalUsers) * 100
              ),
              count: admins,
              color: 'error',
            },
          ]
        : []),
    ];
  }, [
    instructors,
    students,
    totalUsers,
    users,
  ]);

  // ----------------------------------------------------------------------
  // Stats cards
  // ----------------------------------------------------------------------

  const stats = [
    {
      title: 'Total Users',
      value: totalUsers,
      icon: 'solar:users-group-rounded-bold-duotone',
      color: 'primary.main',
      bgcolor: 'primary.lighter',
    },
    {
      title: 'Active Users',
      value: activeUsers,
      icon: 'solar:user-check-bold-duotone',
      color: 'success.main',
      bgcolor: 'success.lighter',
    },
    {
      title: 'Instructors',
      value: instructors,
      icon: 'solar:user-id-bold-duotone',
      color: 'warning.main',
      bgcolor: 'warning.lighter',
    },
    {
      title: 'Students',
      value: students,
      icon: 'solar:backpack-bold-duotone',
      color: 'info.main',
      bgcolor: 'info.lighter',
    },
  ];

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
                color="text.secondary"
                sx={{ fontWeight: 700 }}
              >
                Loading users...
              </Typography>
            </Card>
          </Box>
        )}

        {/* Header */}
        <Box
          sx={{
            mb: 5,
            p: 3,
            borderRadius: 3,
            display: 'flex',
            alignItems: {
              xs: 'flex-start',
              md: 'center',
            },
            justifyContent: 'space-between',
            gap: 2,
            bgcolor: alpha(
              theme.palette.info.main,
              0.06
            ),
            border: '1px solid',
            borderColor: alpha(
              theme.palette.info.main,
              0.12
            ),
          }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1.25,
            }}
          >
            <Box
              sx={{
                width: 40,
                height: 40,
                borderRadius: 2,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: alpha(
                  theme.palette.info.main,
                  0.14
                ),
                color: 'info.main',
              }}
            >
              <Iconify
                icon="solar:users-group-rounded-bold-duotone"
                width={22}
              />
            </Box>

            <Box>
              <Typography
                variant="h4"
                sx={{ fontWeight: 800 }}
              >
                Users Management
              </Typography>

              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mt: 0.5 }}
              >
                Manage users, roles, and access.
              </Typography>
            </Box>
          </Box>

          <Button
            variant="contained"
            startIcon={
              <Iconify icon="solar:user-plus-bold-duotone" />
            }
            onClick={openCreateDialog}
          >
            New User
          </Button>
        </Box>

        {/* Search */}
        <Box
          sx={{
            mb: 3,
            display: 'grid',
            gap: 2,
            gridTemplateColumns: {
              xs: '1fr',
              md: '1fr 260px',
            },
          }}
        >
          <TextField
            label="Search users"
            placeholder="Search by name, email, or role"
            value={query}
            onChange={(event) =>
              setQuery(event.target.value)
            }
            fullWidth
          />

          <Button
            variant="outlined"
            color="inherit"
            onClick={() => setQuery('')}
            sx={{ fontWeight: 800 }}
          >
            Clear
          </Button>
        </Box>

        {/* Stats */}
        <Box
          sx={{
            display: 'grid',
            gap: 3,
            gridTemplateColumns: {
              xs: '1fr',
              sm: '1fr 1fr',
              md: 'repeat(4, 1fr)',
            },
            mb: 5,
          }}
        >
          {stats.map((stat) => (
            <Card
              key={stat.title}
              sx={{
                display: 'flex',
                alignItems: 'center',
                p: 3,
              }}
            >
              <Box sx={{ flexGrow: 1 }}>
                <Typography
                  variant="subtitle2"
                  color="text.secondary"
                >
                  {stat.title}
                </Typography>

                <Typography variant="h3">
                  {stat.value}
                </Typography>
              </Box>

              <Box
                sx={{
                  width: 48,
                  height: 48,
                  borderRadius: 1.5,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  bgcolor: stat.bgcolor,
                  color: stat.color,
                }}
              >
                <Iconify
                  icon={stat.icon}
                  width={24}
                />
              </Box>
            </Card>
          ))}
        </Box>

        {/* Role Distribution */}
        <Box sx={{ mb: 5 }}>
          <Card>
            <CardContent>
              <Typography
                variant="h6"
                sx={{ mb: 3 }}
              >
                Role Distribution
              </Typography>

              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 3,
                }}
              >
                {roleDistribution.map((role) => (
                  <Box key={role.label}>
                    <Box
                      sx={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        mb: 1,
                      }}
                    >
                      <Typography
                        variant="body2"
                        sx={{ fontWeight: 600 }}
                      >
                        {role.label}
                      </Typography>

                      <Typography
                        variant="body2"
                        color="text.secondary"
                      >
                        {role.count} ({role.value}%)
                      </Typography>
                    </Box>

                    <Box
                      sx={{
                        width: '100%',
                        height: 8,
                        bgcolor: 'background.neutral',
                        borderRadius: 4,
                        overflow: 'hidden',
                      }}
                    >
                      <Box
                        sx={{
                          width: `${role.value}%`,
                          height: '100%',
                          bgcolor: `${role.color}.main`,
                        }}
                      />
                    </Box>
                  </Box>
                ))}
              </Box>
            </CardContent>
          </Card>
        </Box>

        {/* Users Table */}
        <Card
          sx={{
            borderRadius: 3,
            overflow: 'hidden',
            border: '1px solid',
            borderColor: 'divider',
          }}
        >
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>User</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Joined</TableCell>
                  <TableCell align="right">
                    Actions
                  </TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {filteredUsers.length === 0 &&
                !isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5}>
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{ py: 2 }}
                      >
                        No users found.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredUsers.map((user) => (
                    <TableRow
                      key={user.id}
                      hover
                    >
                      <TableCell>
                        <Box
                          sx={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 2,
                          }}
                        >
                          <Avatar
                            src={user.avatar}
                            alt={user.name}
                          />

                          <Box>
                            <Typography
                              variant="subtitle2"
                              noWrap
                            >
                              {user.name}
                            </Typography>

                            <Typography
                              variant="body2"
                              color="text.secondary"
                              noWrap
                            >
                              {user.email}
                            </Typography>
                          </Box>
                        </Box>
                      </TableCell>

                      <TableCell>
                        <Chip
                          label={(
                            user.roleName ||
                            user.role
                          ).toUpperCase()}
                          color={getRoleColor(user.role)}
                          size="small"
                          variant="filled"
                        />
                      </TableCell>

                      <TableCell>
                        <Chip
                          label={
                            user.isActive
                              ? 'Active'
                              : 'Inactive'
                          }
                          color={
                            user.isActive
                              ? 'success'
                              : 'default'
                          }
                          size="small"
                          variant="filled"
                        />
                      </TableCell>

                      <TableCell>
                        {user.createdAt.toLocaleDateString()}
                      </TableCell>

                      <TableCell align="right">
                        <Button
                          size="small"
                          color="inherit"
                          onClick={() =>
                            openEditDialog(user)
                          }
                        >
                          <Iconify icon="solar:pen-bold-duotone" />
                        </Button>

                        {user.isActive ? (
                          <Button
                            size="small"
                            color="error"
                            onClick={() =>
                              handleDeleteUser(
                                String(user.id)
                              )
                            }
                          >
                            <Iconify icon="solar:trash-bin-trash-bold-duotone" />
                          </Button>
                        ) : (
                          <Button
                            size="small"
                            color="success"
                            onClick={() =>
                              handleActivateUser(
                                String(user.id)
                              )
                            }
                          >
                            <Iconify icon="solar:user-check-bold-duotone" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Card>

        {/* Dialog */}
        <Dialog
          open={openDialog}
          onClose={handleCloseDialog}
          maxWidth="sm"
          fullWidth
        >
          <Box sx={{ p: 3 }}>
            <Typography
              variant="h6"
              sx={{ mb: 3 }}
            >
              {editingUserId
                ? 'Edit User'
                : 'Add New User'}
            </Typography>

            <Grid container spacing={2}>
              <Grid size={{ xs: 12 }}>
                <TextField
                  label="Full Name"
                  fullWidth
                  value={newUser.name}
                  onChange={(event) =>
                    setNewUser({
                      ...newUser,
                      name: event.target.value,
                    })
                  }
                />
              </Grid>

              <Grid size={{ xs: 12 }}>
                <TextField
                  label="Email"
                  type="email"
                  fullWidth
                  value={newUser.email}
                  onChange={(event) =>
                    setNewUser({
                      ...newUser,
                      email: event.target.value,
                    })
                  }
                />
              </Grid>

              {!editingUserId && (
                <Grid size={{ xs: 12 }}>
                  <TextField
                    label="National ID"
                    fullWidth
                    value={newUser.nationalId}
                    onChange={(event) =>
                      setNewUser({
                        ...newUser,
                        nationalId: event.target.value,
                      })
                    }
                  />
                </Grid>
              )}

              <Grid size={{ xs: 12 }}>
                <TextField
                  label={
                    editingUserId
                      ? 'New Password (optional)'
                      : 'Password'
                  }
                  type="password"
                  fullWidth
                  value={newUser.password}
                  onChange={(event) =>
                    setNewUser({
                      ...newUser,
                      password: event.target.value,
                    })
                  }
                />
              </Grid>

              <Grid size={{ xs: 12 }}>
                <TextField
                  label="Phone"
                  fullWidth
                  value={newUser.phone}
                  onChange={(event) =>
                    setNewUser({
                      ...newUser,
                      phone: event.target.value,
                    })
                  }
                />
              </Grid>

              <Grid size={{ xs: 12 }}>
                <TextField
                  select
                  label="Role"
                  fullWidth
                  value={newUser.role}
                  onChange={(event) =>
                    setNewUser({
                      ...newUser,
                      role: event.target.value as UserRole,
                    })
                  }
                  SelectProps={{
                    native: true,
                  }}
                >
                  <option value="student">
                    Student
                  </option>

                  <option value="instructor">
                    Instructor
                  </option>

                  <option value="engineer">
                    Engineer
                  </option>

                  <option value="board">
                    Board
                  </option>
                </TextField>
              </Grid>

              {editingUserId && (
                <Grid size={{ xs: 12 }}>
                  <TextField
                    select
                    label="Status"
                    fullWidth
                    value={
                      newUser.isActive
                        ? 'active'
                        : 'inactive'
                    }
                    onChange={(event) =>
                      setNewUser({
                        ...newUser,
                        isActive:
                          event.target.value ===
                          'active',
                      })
                    }
                    SelectProps={{
                      native: true,
                    }}
                  >
                    <option value="active">
                      Active
                    </option>

                    <option value="inactive">
                      Inactive
                    </option>
                  </TextField>
                </Grid>
              )}

              <Grid size={{ xs: 12 }}>
                <Box
                  sx={{
                    display: 'flex',
                    justifyContent: 'flex-end',
                    gap: 1,
                    mt: 2,
                  }}
                >
                  <Button
                    onClick={handleCloseDialog}
                    disabled={isSaving}
                  >
                    Cancel
                  </Button>

                  <Button
                    variant="contained"
                    onClick={handleSaveUser}
                    disabled={isSaving}
                  >
                    {isSaving
                      ? 'Saving...'
                      : editingUserId
                        ? 'Save Changes'
                        : 'Add User'}
                  </Button>
                </Box>
              </Grid>
            </Grid>
          </Box>
        </Dialog>
      </Container>
    </DashboardContent>
  );
}