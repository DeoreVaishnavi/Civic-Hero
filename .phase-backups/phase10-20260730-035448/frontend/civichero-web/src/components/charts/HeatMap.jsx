import React from "react"; export default function HeatMap({values=[]}){return <div className="heatmap">{values.map((v,i)=><span className={`h${Math.max(1,Math.min(5,v))}`} key={i}/>)}</div>}
