import type { AxiosResponse } from 'axios';

import http from '../http';

// ----------------------------------------------------------------------

export interface CreateUserRequest {
  email: string;
  password: string;
  nationalId: string;
  fullNameEn: string;
  fullNameAr?: string;
  phone?: string;
}

export interface CreateGenZCodersUserRequest extends CreateUserRequest {
  roleName: 'Student' | 'co-Instructor' | 'Engineer' | 'Board';
}

export interface UpdateUserRequest {
  email: string;
  password?: string;
  nationalId?: string;
  fullNameEn: string;
  fullNameAr?: string;
  phone?: string;
  roleName?: 'Student' | 'co-Instructor' | 'Engineer' | 'Board';
  isActive: boolean;
}

export interface CreateUserResponse {
  message: string;
  id: number;
  email: string;
}

export interface UpdateUserResponse {
  message: string;
  id: number;
}

export interface DeleteUserResponse {
  message: string;
  id: number;
}

export interface UserInfo {
  id: number;
  email: string;
  fullNameEn: string;
  fullNameAr?: string;
  phone?: string;
  roleId: number;
  roleName: string;
  statusId: number;
  statusName: string;
  isActive: boolean;
  isEmailVerified: boolean;
  createdAt: string;
  lastLoginAt?: string;
  hasPassword: boolean;
  hasGoogleAuth: boolean;
}

export interface DashboardSummary {
  totalUsers: number;
  totalStudents: number;
  totalInstructors: number;
  totalAdmins: number;
  activeUsers: number;
  totalCourses: number;
  totalCourseRounds: number;
  totalApplications: number;
  pendingApplications: number;
  totalEnrollments: number;
  recentRegistrations: number;
}

export interface DashboardData {
  summary: DashboardSummary;

  usersByRole: Array<{
    role: string;
    count: number;
  }>;

  recentApplications: Array<{
    id: number;
    courseId: number;
    courseTitle: string;
    fullName: string;
    email?: string;
    price?: number;
    instructorName?: string;
    applicationDate: string;
    statusId: number;
    statusName: string;
  }>;
}

// ----------------------------------------------------------------------

const unwrap = <T>(res: AxiosResponse<T>): T => res.data;

// ----------------------------------------------------------------------

export const adminApi = {
  // ============================================================
  // CREATE INSTRUCTOR
  // ============================================================

  createInstructor: async (
    payload: CreateUserRequest
  ): Promise<CreateUserResponse> => {
    const res = await http.post<CreateUserResponse>(
      '/api/admin/create-instructor',
      payload
    );

    return unwrap(res);
  },

  // ============================================================
  // CREATE ADMIN
  // ============================================================

  createAdmin: async (
    payload: CreateUserRequest
  ): Promise<CreateUserResponse> => {
    const res = await http.post<CreateUserResponse>(
      '/api/admin/create-admin',
      payload
    );

    return unwrap(res);
  },

  // ============================================================
  // CREATE ANY GENZCODERS USER
  // ============================================================

  createUser: async (
    payload: CreateGenZCodersUserRequest
  ): Promise<CreateUserResponse> => {
    const res = await http.post<CreateUserResponse>(
      '/api/admin/create-user',
      payload
    );

    return unwrap(res);
  },

  // ============================================================
  // UPDATE USER
  // ============================================================

  updateUser: async (
    id: number,
    payload: UpdateUserRequest
  ): Promise<UpdateUserResponse> => {
    const res = await http.put<UpdateUserResponse>(
      `/api/admin/users/${id}`,
      payload
    );

    return unwrap(res);
  },

  // ============================================================
  // DELETE / DEACTIVATE USER
  // ============================================================

  deleteUser: async (id: number): Promise<DeleteUserResponse> => {
    const res = await http.delete<DeleteUserResponse>(
      `/api/admin/users/${id}`
    );

    return unwrap(res);
  },

  // ============================================================
  // ACTIVATE USER
  // ============================================================

  activateUser: async (id: number): Promise<DeleteUserResponse> => {
    const res = await http.patch<DeleteUserResponse>(
      `/api/admin/users/${id}/activate`
    );

    return unwrap(res);
  },

  // ============================================================
  // GET ALL USERS
  // ============================================================

  getAllUsers: async (): Promise<UserInfo[]> => {
    const res = await http.get<UserInfo[]>('/api/admin/users');

    return unwrap(res);
  },

  // ============================================================
  // GET DASHBOARD
  // ============================================================

  getDashboard: async (): Promise<DashboardData> => {
    const res = await http.get<DashboardData>('/api/admin/dashboard');

    return unwrap(res);
  },
};