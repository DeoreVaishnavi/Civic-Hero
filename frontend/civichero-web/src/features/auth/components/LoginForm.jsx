import { useState } from "react";
import PropTypes from "prop-types";
import { zodResolver } from "@hookform/resolvers/zod";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { Alert, Button, IconButton, InputAdornment, TextField } from "@mui/material";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { loginSchema } from "../validation/authSchemas";

export default function LoginForm({ onSubmit, error }) {
  const { t } = useTranslation("auth");
  const [showPassword, setShowPassword] = useState(false);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({ resolver: zodResolver(loginSchema) });

  return <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
    {error && <Alert severity="error">{error}</Alert>}
    <TextField fullWidth label={t("email")} autoComplete="username" {...register("email")} error={!!errors.email} helperText={errors.email?.message} />
    <TextField
      fullWidth
      type={showPassword ? "text" : "password"}
      label={t("password")}
      autoComplete="current-password"
      {...register("password")}
      error={!!errors.password}
      helperText={errors.password?.message}
      slotProps={{ input: { endAdornment: <InputAdornment position="end"><IconButton aria-label={showPassword ? "Hide password" : "Show password"} edge="end" onClick={() => setShowPassword((visible) => !visible)} onMouseDown={(event) => event.preventDefault()}>{showPassword ? <VisibilityOff /> : <Visibility />}</IconButton></InputAdornment> } }}
    />
    <Button fullWidth type="submit" variant="contained" disabled={isSubmitting}>{t("signIn")}</Button>
  </form>;
}

LoginForm.propTypes = { onSubmit: PropTypes.func.isRequired, error: PropTypes.string };
