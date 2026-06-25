import { lazy } from "react";
import {
	createBrowserRouter,
	createRoutesFromElements,
	Route,
} from "react-router-dom";

import MainLayout from "@layouts/MainLayout";
import ProtectedRouter from "@routes/ProtectedRouter";
import App from "@pages/App"; // landing page loads eagerly (most-visited route)

// Everything else is code-split so a route only ships its own JS.
const Login = lazy(() => import("@pages/Login"));
const Register = lazy(() => import("@pages/Register"));
const Notes = lazy(() => import("@pages/Notes"));
const Settings = lazy(() => import("@pages/Settings"));
const NotFound = lazy(() => import("@pages/NotFound"));

export const router = createBrowserRouter(
	createRoutesFromElements(
		<Route element={<MainLayout />}>
			{/* Public */}
			<Route index element={<App />} />
			<Route path="login" element={<Login />} />
			<Route path="register" element={<Register />} />

			{/* Authenticated */}
			<Route element={<ProtectedRouter />}>
				<Route path="notes" element={<Notes />} />
				<Route path="settings" element={<Settings />} />
			</Route>

			<Route path="*" element={<NotFound />} />
		</Route>,
	),
);
