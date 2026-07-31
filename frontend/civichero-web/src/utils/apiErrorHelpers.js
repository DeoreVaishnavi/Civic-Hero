export const getApiError=(error,fallback="Something went wrong.")=>error?.response?.data?.message||Object.values(error?.response?.data?.errors||{})?.flat()?.[0]?.message||error?.message||fallback;
