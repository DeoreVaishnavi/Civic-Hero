import { useState } from "react";
import { Box, Card, CardContent, ToggleButton,ToggleButtonGroup,Typography } from "@mui/material";
import { Grain, LocationOn } from "@mui/icons-material";
import { useQuery } from "@tanstack/react-query";
import { analyticsApi } from "../../../api/analyticsApi";
import ComplaintHeatMap from "../../../components/maps/ComplaintHeatMap";
import ComplaintMarkerMap from "../../../components/maps/ComplaintMarkerMap";
import PageHeader from "../../../components/common/PageHeader";
import ComplaintFilters from "../../complaints/components/ComplaintFilters";
export default function HeatMapPage(){const[view,setView]=useState("heat"),[filters,setFilters]=useState({});const{data=[]}=useQuery({queryKey:["analytics","heatmap",filters],queryFn:()=>analyticsApi.heatmap(filters)});const points=data.points||data;return <><PageHeader title="Complaint density intelligence" description="Privacy-aware spatial analysis for resource planning and ward-level decisions." action={<ToggleButtonGroup exclusive value={view} onChange={(_,value)=>value&&setView(value)}><ToggleButton value="heat"><Grain sx={{mr:1}}/>Heat map</ToggleButton><ToggleButton value="markers"><LocationOn sx={{mr:1}}/>Markers</ToggleButton></ToggleButtonGroup>}/><Card sx={{mb:3}}><CardContent><ComplaintFilters value={filters} onChange={setFilters}/></CardContent></Card><Card className="overflow-hidden"><Box className="flex items-center justify-between border-b border-civic-border px-5 py-3"><div><Typography fontWeight={900}>{view==="heat"?"Density overlay":"Complaint locations"}</Typography><Typography variant="caption" color="text.secondary">{points.length||0} spatial data points</Typography></div></Box><CardContent sx={{p:1}}>{view==="heat"?<ComplaintHeatMap points={points}/>:<ComplaintMarkerMap complaints={points}/>}</CardContent></Card></>}
