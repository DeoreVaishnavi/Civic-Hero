export const disputeApi={create:async data=>({id:`DSP-${Date.now()}`,status:"Submitted",...data}),list:async()=>[]};
