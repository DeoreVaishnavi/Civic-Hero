export const verificationApi={verifyEmail:async token=>({verified:Boolean(token)}),resend:async email=>({email,sent:true})};
