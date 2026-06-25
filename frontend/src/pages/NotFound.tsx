import { useNavigate } from "react-router-dom";
import { Home } from "lucide-react";
import { Button } from "@components/ui/Button";

export default function NotFound() {
	const navigate = useNavigate();
	return (
		<div className="page">
			<div className="flex min-h-[55vh] flex-col items-center justify-center text-center">
				<p className="bg-gradient-to-r from-primary to-blue-400 bg-clip-text font-mono text-7xl font-bold tracking-tight text-transparent sm:text-8xl">
					404
				</p>
				<h1 className="mt-4 text-2xl font-semibold sm:text-3xl">This page wandered off.</h1>
				<p className="mx-auto mt-2 max-w-md text-balance text-muted-foreground">
					The link may be broken, or the page may have moved.
				</p>
				<div className="mt-8">
					<Button onClick={() => navigate("/")}>
						<Home className="h-4 w-4" />
						Back home
					</Button>
				</div>
			</div>
		</div>
	);
}
