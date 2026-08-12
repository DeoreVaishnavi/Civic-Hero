export async function refreshAccessToken(){ return { accessToken:"demo-access-token", expiresIn:3600 }; }
export function scheduleTokenRefresh(callback, milliseconds=45*60*1000){ const id=setTimeout(callback,milliseconds); return ()=>clearTimeout(id); }
