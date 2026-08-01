import { useState } from "react";
import PropTypes from "prop-types";
import { zodResolver } from "@hookform/resolvers/zod";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { Alert, Button, IconButton, InputAdornment, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { registerSchema } from "../validation/authSchemas";

function PasswordField({ name, label, register, error }) {
  const [visible, setVisible] = useState(false);
  return <TextField
    fullWidth
    type={visible ? "text" : "password"}
    label={label}
    autoComplete="new-password"
    {...register(name)}
    error={!!error}
    helperText={error?.message}
    slotProps={{ input: { endAdornment: <InputAdornment position="end"><IconButton aria-label={visible ? `Hide ${label}` : `Show ${label}`} edge="end" onClick={() => setVisible((current) => !current)} onMouseDown={(event) => event.preventDefault()}>{visible ? <VisibilityOff /> : <Visibility />}</IconButton></InputAdornment> } }}
  />;
}

PasswordField.propTypes = { name: PropTypes.string.isRequired, label: PropTypes.string.isRequired, register: PropTypes.func.isRequired, error: PropTypes.object };

export default function RegisterForm({ onSubmit, error }) {
  const { t } = useTranslation("auth");
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({ resolver: zodResolver(registerSchema) });
  return <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
    {error && <Alert severity="error">{error}</Alert>}
    <TextField fullWidth label={t("fullName")} autoComplete="name" {...register("fullName")} error={!!errors.fullName} helperText={errors.fullName?.message} />
    <TextField fullWidth type="email" label={t("email")} autoComplete="email" {...register("email")} error={!!errors.email} helperText={errors.email?.message} />
    <TextField fullWidth type="tel" label={t("phoneNumber")} autoComplete="tel" {...register("phoneNumber")} error={!!errors.phoneNumber} helperText={errors.phoneNumber?.message} />
    <TextField fullWidth type="password" label={t("password")} autoComplete="new-password" {...register("password")} error={!!errors.password} helperText={errors.password?.message} />
    <PasswordField name="confirmPassword" label={t("confirmPassword")} register={register} error={errors.confirmPassword} />
    <Button fullWidth type="submit" variant="contained" disabled={isSubmitting}>{t("createAccount")}</Button>
  </form>;
}

RegisterForm.propTypes = { onSubmit: PropTypes.func.isRequired, error: PropTypes.string };
