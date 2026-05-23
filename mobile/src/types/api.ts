export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  id: number;
  displayName: string;
  token: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
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
