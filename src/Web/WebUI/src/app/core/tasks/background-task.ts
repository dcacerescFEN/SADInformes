export type BackgroundTaskStatus = 'queued' | 'running' | 'completed' | 'partial' | 'failed' | 'cancelled';

export interface BackgroundTaskDetail {
  label: string;
  value: string;
}

export interface BackgroundTask {
  id: string;
  kind: string;
  title: string;
  description?: string;
  status: BackgroundTaskStatus;
  current: number;
  total?: number;
  details: BackgroundTaskDetail[];
  stage?: string;
  terminal: boolean;
  resumable?: boolean;
  cancel?: () => void;
  cancelLabel?: string;
}

export interface BackgroundTaskPatch {
  status?: BackgroundTaskStatus;
  current?: number;
  total?: number;
  details?: BackgroundTaskDetail[];
  stage?: string;
  terminal?: boolean;
}

import type { Observable } from 'rxjs';

export type BackgroundTaskPoll = () => Observable<BackgroundTaskPatch>;
