import { isAxiosError } from 'axios';

export const getErrorMessage = (err: unknown, fallback: string): string =>
  err instanceof Error ? err.message : fallback;

interface AxiosErrorWithServerError {
  response?: {
    status: number;
    data?: unknown;
  };
  _serverError?: string;
  code?: string;
}

interface AxiosResponseData {
  error?: string;
  errors?: { errors?: string[] };
}

/** HTTP status from an axios error, or undefined for non-axios errors. */
export const getServerErrorStatus = (err: unknown): number | undefined => {
  if (!isAxiosError(err)) return undefined;
  return (err as AxiosErrorWithServerError).response?.status;
};

/** Field-validation messages from a 400 ValidationProblemDetails response. */
export const getServerValidationErrors = (err: unknown): string[] => {
  if (!isAxiosError(err)) return [];
  const axiosErr = err as AxiosErrorWithServerError & {
    response?: { data?: AxiosResponseData };
  };
  return axiosErr.response?.data?.errors?.errors ?? [];
};

/**
 * Map axios errors to user-friendly messages for screens.
 * Highest-signal first: server-provided message → network → authZ → authN → server → fallback.
 */
export const getFriendlyErrorMessage = (
  err: unknown,
  fallback: string,
): string => {
  if (isAxiosError(err)) {
    const axiosErr = err as AxiosErrorWithServerError & {
      response?: { data?: AxiosResponseData };
    };

    // Server-provided message outranks generic status text
    if (axiosErr._serverError) {
      return axiosErr._serverError;
    }
    if (axiosErr.response?.data?.error) {
      return axiosErr.response.data.error;
    }

    // Network error (no response received)
    if (!axiosErr.response) {
      return 'Connection failed — check your network';
    }

    // 401 Unauthorized
    if (axiosErr.response.status === 401) {
      return 'Your session expired — please sign in again';
    }

    // 403 Forbidden
    if (axiosErr.response.status === 403) {
      return "You don't have permission to do that";
    }

    // 500+ generic server error
    if (axiosErr.response.status >= 500) {
      return 'Server error — please try again later';
    }
  }

  return fallback;
};

export const getDownloadErrorMessage = (err: unknown): string => {
  if (isAxiosError(err)) {
    const axiosErr = err as AxiosErrorWithServerError;

    // Network error (no response received)
    if (!axiosErr.response) {
      return 'Connection failed — check your network';
    }

    // 500 Internal Server Error: generic server failure
    if (axiosErr.response.status === 500) {
      return 'Server error — please try again later';
    }
  }

  // Handle ReactNativeBlobUtil / fetch errors (non-Axios)
  if (err instanceof Error) {
    const msg = err.message.toLowerCase();
    if (
      msg.includes('cert') ||
      msg.includes('ssl') ||
      msg.includes('handshake')
    ) {
      return 'SSL connection failed — check your network or certificate';
    }
    if (
      msg.includes('network') ||
      msg.includes('econnrefused') ||
      msg.includes('timeout')
    ) {
      return 'Connection failed — check your network';
    }
    return err.message;
  }

  return 'Download failed';
};
