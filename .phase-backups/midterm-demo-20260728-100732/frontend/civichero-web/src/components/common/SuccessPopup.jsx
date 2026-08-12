import React from "react"; export default function SuccessPopup({message,visible}){return visible?<div className="toast">✓ {message}</div>:null}
