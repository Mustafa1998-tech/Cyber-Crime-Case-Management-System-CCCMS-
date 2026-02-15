export interface AuthResponse {
  mfaRequired: boolean;
  message: string;
  accessToken?: string;
  refreshToken?: string;
  accessTokenExpiresAtUtc?: string;
  userName: string;
  roles: string[];
}

export interface UserSession {
  accessToken: string;
  refreshToken: string;
  userName: string;
  roles: string[];
  accessTokenExpiresAtUtc?: string;
}
