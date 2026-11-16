// Public Entra configuration; tokens stay in MSAL's in-memory cache.
import { PublicClientApplication, InteractionRequiredAuthError } from '@azure/msal-browser';
const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID || '';
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID || '';
const scope = import.meta.env.VITE_ENTRA_SCOPE || '';
export const authenticationEnabled = Boolean(clientId && tenantId && scope);
let application: Promise<PublicClientApplication> | undefined;
async function identity() {
  return application ??= (async () => {
    const app = new PublicClientApplication({auth:{clientId, authority:`https://login.microsoftonline.com/${tenantId}`, redirectUri:location.origin}, cache:{cacheLocation:'memoryStorage'}});
    await app.initialize();
    return app;
  })();
}
export async function signIn() {
  const app = await identity();
  const response = await app.loginPopup({scopes:[scope]});
  app.setActiveAccount(response.account);
}
export async function authorizedFetch(url: string, options?: RequestInit): Promise<Response> {
  if (!authenticationEnabled) return fetch(url, options);
  const app = await identity();
  const account = app.getActiveAccount();
  if (!account) throw new Error('Sign in to Azure before connecting to the hosted API.');
  try {
    const token = await app.acquireTokenSilent({account, scopes:[scope]});
    const headers = new Headers(options?.headers);
    headers.set('Authorization', `Bearer ${token.accessToken}`);
    return fetch(url, {...options, headers});
  } catch(error) {
    if (error instanceof InteractionRequiredAuthError) throw new Error('Your Azure session expired. Sign in again.');
    throw error;
  }
}
