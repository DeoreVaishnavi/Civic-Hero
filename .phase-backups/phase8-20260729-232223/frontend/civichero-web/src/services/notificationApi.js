import {notifications} from "../demoData.js"; export const notificationApi={list:async()=>notifications,markRead:async id=>({id,read:true})};
