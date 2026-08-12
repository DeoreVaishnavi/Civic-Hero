import {users} from "../demoData.js"; export const userApi={list:async()=>users,updateRole:async(id,role)=>({id,role}),updateScope:async(id,scope)=>({id,scope})};
