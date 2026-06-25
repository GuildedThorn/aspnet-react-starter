import "@styles/index.css";

import { Suspense } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import { router } from "@routes/AppRoutes";
import { ErrorBoundary } from "@components/ErrorBoundary";
import { AuthProvider } from "@components/AuthContext";
import { applyTheme, getStoredTheme } from "@lib/theme";

// Apply the saved theme as the bundle loads (no render-blocking inline script).
applyTheme(getStoredTheme());

createRoot(document.getElementById("root")!).render(
	<ErrorBoundary>
		<AuthProvider>
			<Suspense
				fallback={
					<div className="flex min-h-[60vh] items-center justify-center">
						<div
							className="h-8 w-8 animate-spin rounded-full border-2 border-border border-t-primary"
							role="status"
							aria-label="Loading"
						/>
					</div>
				}
			>
				<RouterProvider router={router} />
			</Suspense>
		</AuthProvider>
	</ErrorBoundary>,
);
