import type {
  LoginUserRequest,
  LoginUserResponse,
  MeResponse,
  RegisterAnonymousRequest,
  RegisterAnonymousResponse,
  RegisterUserRequest,
  RegisterUserResult,
  RequestPasswordResetRequest,
  ResendVerificationRequest,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from "../../../types";
import { api } from "../../shared/api/api";

const invalidatesMeOnSuccess = (_result: unknown, error: unknown) =>
  error ? [] : ["Me" as const];

export const authApi = api.injectEndpoints({
  endpoints: (builder) => ({
    me: builder.query<MeResponse, void>({
      query: () => "/api/auth/me",
      providesTags: ["Me"],
    }),
    login: builder.mutation<LoginUserResponse, LoginUserRequest>({
      query: (body) => ({ url: "/api/auth/login", method: "POST", body }),
      // a 401 here means wrong credentials, not an expired session
      extraOptions: { reauth: false },
      invalidatesTags: invalidatesMeOnSuccess,
    }),
    register: builder.mutation<RegisterUserResult, RegisterUserRequest>({
      query: (body) => ({ url: "/api/auth/register", method: "POST", body }),
    }),
    guest: builder.mutation<
      RegisterAnonymousResponse,
      RegisterAnonymousRequest
    >({
      query: (body) => ({ url: "/api/auth/guest", method: "POST", body }),
      invalidatesTags: invalidatesMeOnSuccess,
    }),
    logout: builder.mutation<void, void>({
      query: () => ({ url: "/api/auth/logout", method: "POST" }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled;
        } catch {
          return;
        }
        dispatch(api.util.resetApiState());
      },
    }),
    forgotPassword: builder.mutation<void, RequestPasswordResetRequest>({
      query: (body) => ({
        url: "/api/auth/forgot-password",
        method: "POST",
        body,
      }),
    }),
    resetPassword: builder.mutation<void, ResetPasswordRequest>({
      query: (body) => ({
        url: "/api/auth/reset-password",
        method: "POST",
        body,
      }),
    }),
    resendVerification: builder.mutation<void, ResendVerificationRequest>({
      query: (body) => ({
        url: "/api/auth/resend-verification",
        method: "POST",
        body,
      }),
    }),
    verifyEmail: builder.mutation<void, VerifyEmailRequest>({
      query: (body) => ({
        url: "/api/auth/verify-email",
        method: "POST",
        body,
      }),
    }),
  }),
});

export const {
  useMeQuery,
  useLoginMutation,
  useRegisterMutation,
  useGuestMutation,
  useLogoutMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
  useResendVerificationMutation,
  useVerifyEmailMutation,
} = authApi;
