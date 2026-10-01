export interface CurrentUser {
  id: number;
  email: string;
  dealership: { id: number; name: string };
  roles: string[];
}
export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest extends LoginRequest {
  confirmPassword: string;
  dealershipName: string;
}
