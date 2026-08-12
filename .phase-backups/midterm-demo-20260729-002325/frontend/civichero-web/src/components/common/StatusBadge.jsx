import React from "react"; export default function StatusBadge({status="Submitted"}){return <span className={`status ${status.toLowerCase().replace(/\s+/g,"-")}`}><i/>{status}</span>}
