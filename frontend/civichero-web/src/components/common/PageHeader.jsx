import PropTypes from "prop-types";
import { Box, Typography } from "@mui/material";
export default function PageHeader({title,description,action}){return <header className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div className="min-w-0"><Box className="mb-2 h-1 w-12 rounded-full bg-creamsoda-500"/><Typography component="h1" variant="h4" sx={{fontWeight:900}}>{title}</Typography>{description&&<Typography color="text.secondary" sx={{mt:.75,maxWidth:760,lineHeight:1.6}}>{description}</Typography>}</div>{action&&<div className="flex shrink-0 flex-wrap gap-2">{action}</div>}</header>}
PageHeader.propTypes={title:PropTypes.string.isRequired,description:PropTypes.string,action:PropTypes.node};
