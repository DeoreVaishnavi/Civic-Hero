export const authApi={login:async({email,role="Citizen"})=>({accessToken:"demo-token",user:{email,role}}),logout:async()=>true,me:async()=>({name:"Demo User",role:"Citizen"})};
