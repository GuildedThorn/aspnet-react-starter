import { useNavigate } from "react-router-dom";
import { useAuth } from "@components/AuthContext";
import { Button } from "@components/ui/Button";

const stack = [
	"ASP.NET Core 10 API",
	"React + TypeScript + Vite",
	"Tailwind CSS",
	"MongoDB",
	"JWT cookie + WebAuthn auth",
	"SignalR realtime",
	"RabbitMQ messaging",
	"Nix flake · Docker · CI",
];

export default function App() {
	const { isAuthenticated, loading } = useAuth();
	const navigate = useNavigate();

	return (
		<div className="mx-auto max-w-3xl px-4 py-16 text-center">
			<h1 className="text-4xl font-extrabold tracking-tight sm:text-5xl">
				Full-stack starter
			</h1>
			<p className="mx-auto mt-4 max-w-xl text-lg text-muted-foreground">
				An ASP.NET Core 10 + React template wired with auth, MongoDB, realtime,
				and a flake-native deploy. Register an account and open{" "}
				<span className="font-medium text-foreground">Notes</span> to see the
				example CRUD (REST + SignalR live updates) in action.
			</p>

			<div className="mt-8 flex justify-center gap-3">
				{!loading && isAuthenticated ? (
					<Button onClick={() => navigate("/notes")}>Open Notes</Button>
				) : (
					<>
						<Button onClick={() => navigate("/register")}>Get started</Button>
						<Button variant="outline" onClick={() => navigate("/login")}>
							Login
						</Button>
					</>
				)}
			</div>

			<ul className="mx-auto mt-12 grid max-w-xl grid-cols-2 gap-2 text-left text-sm">
				{stack.map((s) => (
					<li key={s} className="rounded-lg border border-border bg-card px-3 py-2">
						{s}
					</li>
				))}
			</ul>
		</div>
	);
}
