export const roleHome=role=>({Citizen:"/citizen/dashboard",Officer:"/officer/dashboard",Supervisor:"/supervisor/dashboard",Admin:"/admin/dashboard",SuperAdmin:"/admin/dashboard"}[role]||"/login");
