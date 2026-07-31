import PropTypes from "prop-types";
import { InboxOutlined } from "@mui/icons-material";
import { Box, Typography } from "@mui/material";
export default function EmptyState({title="No records found",description}){return <div className="grid min-h-64 place-items-center rounded-[24px] border border-dashed border-civic-border bg-white/80 p-8 text-center"><div><Box className="mx-auto mb-3 grid h-16 w-16 place-items-center rounded-2xl bg-blueberry-50 text-blueberry-600"><InboxOutlined sx={{fontSize:34}}/></Box><Typography variant="h6">{title}</Typography>{description&&<Typography color="text.secondary" sx={{mt:1,maxWidth:440}}>{description}</Typography>}</div></div>}
EmptyState.propTypes={title:PropTypes.string,description:PropTypes.string};
