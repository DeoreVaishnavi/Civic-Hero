import PropTypes from "prop-types";
import { NotificationsActiveOutlined } from "@mui/icons-material";
import { Avatar, ListItemButton, ListItemText } from "@mui/material";
import { formatDateTime } from "../../utils/dateFormatter";
export default function NotificationItem({notification,onClick}){return <ListItemButton onClick={onClick} sx={{m:1,borderRadius:3,border:"1px solid",borderColor:notification.isRead?"transparent":"primary.light",bgcolor:notification.isRead?"transparent":"primary.light"}}><Avatar sx={{mr:2,bgcolor:notification.isRead?"grey.100":"primary.main",color:notification.isRead?"text.secondary":"white"}}><NotificationsActiveOutlined/></Avatar><ListItemText primary={notification.title||notification.message} secondary={`${notification.message&&notification.title?notification.message+" · ":""}${formatDateTime(notification.createdAt)}`} primaryTypographyProps={{fontWeight:notification.isRead?600:850}}/></ListItemButton>}
NotificationItem.propTypes={notification:PropTypes.object.isRequired,onClick:PropTypes.func};
