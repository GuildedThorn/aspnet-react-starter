import { Outlet } from "react-router-dom";
import NavBar from "@components/ui/NavBar";
import Footer from "@components/ui/Footer";

export default function MainLayout() {
	return (
		<div className="flex min-h-dvh flex-col bg-background text-foreground">
			<a
				href="#main-content"
				className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-50 focus:rounded-md focus:bg-primary focus:px-4 focus:py-2 focus:font-medium focus:text-primary-foreground focus:shadow"
			>
				Skip to content
			</a>

			<header>
				<NavBar />
			</header>

			<main id="main-content" tabIndex={-1} className="flex-grow scroll-mt-20 focus:outline-none">
				<Outlet />
			</main>

			<Footer />
		</div>
	);
}
