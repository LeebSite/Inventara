export const TOKEN_KEY = "inventara.accessToken"
export const USER_KEY = "inventara.user"

export function clearStoredAuth() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}
