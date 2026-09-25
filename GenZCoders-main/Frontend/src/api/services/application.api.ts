import type { AxiosResponse } from 'axios';

import http from '../http';

import type {
  ApplicationDto,
  CreateApplicationRequest,
  UpdateApplicationStatusRequest,
  UpdateApplicationCourseRoundRequest,
} from '../models/application';

const unwrap = <T>(res: AxiosResponse<T>): T => res.data;

export const applicationApi = {
  getApplications: async (): Promise<ApplicationDto[]> => {
    const res = await http.get<ApplicationDto[]>('/api/Application');
    return unwrap(res);
  },

  updateApplicationCourseRound: async (
    id: string | number,
    payload: UpdateApplicationCourseRoundRequest
  ): Promise<void> => {
    await http.patch(`/api/Application/${id}/course-round`, payload, {
      validateStatus: (status: number) => (status >= 200 && status < 300) || status === 204,
    });
  },

  getApplicationById: async (id: string | number): Promise<ApplicationDto> => {
    const res = await http.get<ApplicationDto>(`/api/Application/${id}`);
    return unwrap(res);
  },

  createApplication: async (payload: CreateApplicationRequest, accountId?: number): Promise<ApplicationDto> => {
    const params = accountId ? { accountId } : {};
    const res = await http.post<ApplicationDto>('/api/Application', payload, { params });
    return unwrap(res);
  },

  updateApplicationStatus: async (id: string | number, payload: UpdateApplicationStatusRequest): Promise<ApplicationDto | void> => {
    try {
      const res = await http.patch<ApplicationDto>(`/api/Application/${id}/status`, payload, {
        validateStatus: (status: number) => (status >= 200 && status < 300) || status === 204,
      });

      return res.status === 204 ? undefined : res.data;
    } catch (error: any) {
      // Log the error for debugging
      console.error('PATCH /api/Application error:', {
        id,
        payload,
        error: error?.response?.data || error?.message,
        status: error?.response?.status,
      });
      throw error;
    }
  },

  deleteApplication: async (id: string | number): Promise<void> => {
    await http.delete(`/api/Application/${id}`, {
      validateStatus: (status: number) => (status >= 200 && status < 300) || status === 204,
    });
  },
};
