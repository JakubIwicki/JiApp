export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  /** Mapped from API `accessToken` field */
  token: string;
  /** Mapped from API `userId` field */
  id: number;
  displayName: string;
  /** Module ids the user is granted (e.g. ["YtDownloader","Scheduler"]). */
  modules: string[];
}

/** Raw shape of POST /auth/login response. */
export interface LoginApiRaw {
  userId: number;
  displayName: string;
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  modules?: string[];
}

/** Raw shape of GET /auth/me response. */
export interface MeApiRaw {
  id: number;
  displayName?: string;
  username?: string;
  email?: string;
  modules?: string[];
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  email: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/** Raw shape of PATCH /auth/profile response. */
export interface UpdateProfileApiRaw {
  id: number;
  displayName?: string;
  username?: string;
  email?: string;
}

export interface SearchRequest {
  query: string;
  maxResults?: number;
}

export interface VideoItem {
  videoId: string;
  title: string;
  description: string;
  imageUrl: string;
  videoUrl: string;
  channelTitle: string;
}

export interface SearchResponse {
  results: VideoItem[];
}

export interface DownloadRequest {
  videoId: string;
  videoUrl: string;
  title?: string;
  description?: string;
  imageUrl?: string;
}

export interface DownloadResponse {
  downloadUrl: string;
}

export interface SearchHistoryItem {
  id: number;
  searchText: string;
  searchedAt: string;
}

export interface DownloadHistoryItem {
  id: number;
  videoTitle: string;
  videoDescription: string;
  videoId: string;
  videoUrl: string;
  imageUrl: string;
  downloadedAt: string;
}

export interface ApiErrorResponse {
  error: string;
  details?: string;
  retryAfterSeconds?: string;
}

export interface HistoryResponse {
  searches: SearchHistoryItem[];
  downloads: DownloadHistoryItem[];
}
