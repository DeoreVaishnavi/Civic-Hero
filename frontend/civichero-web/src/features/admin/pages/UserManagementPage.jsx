import { useState } from "react";
import PropTypes from "prop-types";
import { AdminPanelSettings, PersonAddAlt } from "@mui/icons-material";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, TextField, Typography } from "@mui/material";
import { useMutation,useQuery,useQueryClient } from "@tanstack/react-query";
import { userApi } from "../../../api/userApi";
import PageHeader from "../../../components/common/PageHeader";
import { notify } from "../../../utils/notify";
import ManagementTable from "../components/ManagementTable";

export default function UserManagementPage({staffOnly=false}){
  const [selected,setSelected]=useState(null);
  const [role,setRole]=useState("Citizen");
  const [departmentId,setDepartmentId]=useState("");
  const [wardId,setWardId]=useState("");
  const qc=useQueryClient();
  const key=["admin","users",staffOnly];
  const{data={}}=useQuery({queryKey:key,queryFn:()=>userApi.list({role:staffOnly?"Officer":undefined,pageSize:100})});
  const{data:metadata={}}=useQuery({queryKey:["admin","user-metadata"],queryFn:userApi.metadata});
  const refresh=()=>qc.invalidateQueries({queryKey:["admin","users"]});
  const act=async(id,active)=>{await(active?userApi.activate(id):userApi.deactivate(id));notify(active?"Account activated":"Account deactivated");refresh()};
  const changeRole=useMutation({mutationFn:()=>userApi.changeRole(selected.id,{role,departmentId:departmentId?Number(departmentId):null,wardId:wardId?Number(wardId):null}),onSuccess:()=>{notify(`${selected.fullName} is now ${role}. Existing sessions were revoked.`);setSelected(null);refresh()},onError:error=>notify(error.response?.data?.message||"Unable to change role","error")});
  const openRole=(user)=>{setSelected(user);setRole(user.role||"Citizen");setDepartmentId(user.departmentId||"");setWardId(user.wardId||"")};
  const scoped=["Officer","Supervisor"].includes(role);
  const departments=metadata.departments||[];
  const wards=(metadata.wards||[]).filter(item=>!departmentId||item.departmentId===Number(departmentId));
  return <><PageHeader title={staffOnly?"Staff management":"User and role management"} description={staffOnly?"Manage municipal officers and their operational access.":"Promote a verified citizen to Admin or Staff here. Public registration always creates Citizen accounts."} action={!staffOnly&&<Button startIcon={<PersonAddAlt/>} variant="outlined" disabled>Register citizen first, then promote</Button>}/><Alert severity="info" icon={<AdminPanelSettings/>} sx={{mb:3}}>To create an Admin: register and verify the person as a Citizen, then select Edit beside their account and change the role to Admin.</Alert><ManagementTable rows={data.items||data||[]} columns={[{key:"fullName",label:"Name"},{key:"email",label:"Email"},{key:"role",label:"Role"},{key:"isEmailVerified",label:"Email verified"},{key:"isActive",label:"Active"}]} onDeactivate={id=>act(id,false)} onActivate={id=>act(id,true)} onEdit={openRole}/><Dialog open={!!selected} onClose={()=>setSelected(null)}><DialogTitle>Change role and access</DialogTitle><DialogContent className="space-y-4" sx={{pt:"12px!important"}}><Typography><strong>{selected?.fullName}</strong><br/>{selected?.email}</Typography><Alert severity="warning">Changing role immediately revokes this user’s existing sessions.</Alert><TextField fullWidth select label="Role" value={role} onChange={event=>{setRole(event.target.value);setDepartmentId("");setWardId("")}}>{["Citizen","Officer","Supervisor","Admin"].map(value=><MenuItem key={value} value={value}>{value}</MenuItem>)}</TextField>{scoped&&<><TextField fullWidth select label="Department" value={departmentId} onChange={event=>{setDepartmentId(event.target.value);setWardId("")}}>{departments.map(item=><MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}</TextField><TextField fullWidth select label="Ward" value={wardId} onChange={event=>setWardId(event.target.value)}><MenuItem value="">All department wards</MenuItem>{wards.map(item=><MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}</TextField></>}</DialogContent><DialogActions><Button onClick={()=>setSelected(null)}>Cancel</Button><Button variant="contained" disabled={changeRole.isPending||(scoped&&!departmentId)} onClick={()=>changeRole.mutate()}>Save role</Button></DialogActions></Dialog></>;
}
UserManagementPage.propTypes={staffOnly:PropTypes.bool};
