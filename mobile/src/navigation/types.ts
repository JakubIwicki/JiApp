export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

export type MainStackParamList = {
  Search: undefined;
  Download: { videoId: string; title: string; description: string; imageUrl: string; videoUrl: string };
  History: undefined;
  Settings: undefined;
};
