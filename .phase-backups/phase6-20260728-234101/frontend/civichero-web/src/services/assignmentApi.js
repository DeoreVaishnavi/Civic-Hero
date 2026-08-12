export const assignmentApi={list:async()=>[],assign:async(id,officerId)=>({id,officerId,status:"Assigned"}),escalate:async id=>({id,status:"Escalated"})};
