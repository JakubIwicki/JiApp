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
  nextPageToken: string | null;
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

export interface HistoryResponse {
  searches: SearchHistoryItem[];
  downloads: DownloadHistoryItem[];
}
