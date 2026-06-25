import { Link, NavLink } from "react-router-dom";
import { LogIn, LogOut, UserPlus, StickyNote, Settings } from "lucide-react";
import { cn } from "@lib/utils";
import { useAuth } from "@components/AuthContext";
import { ThemeToggle } from "@components/ui/ThemeToggle";
import { logout } from "@backend/api";

const linkClass = ({ isActive }: { isActive: boolean }) =>
	cn("nav-link", isActive && "bg-muted text-primary");

export default function NavBar() {
	const { isAuthenticated, user, loading, refresh } = useAuth();

	const handleLogout = async () => {
		await logout();
		await refresh();
	};

	return (
		<div className="sticky top-0 z-50 px-3 pt-3">
			<nav className="mx-auto flex h-16 max-w-5xl items-center justify-between gap-2 rounded-2xl border border-border bg-card/80 px-4 shadow-sm backdrop-blur sm:px-6">
				<Link to="/" className="text-xl font-extrabold tracking-tight text-primary">
					App
				</Link>

				<div className="flex items-center gap-1">
					{isAuthenticated && (
						<>
							<NavLink to="/notes" className={linkClass}>
								<StickyNote className="h-4 w-4" />
								<span className="hidden sm:inline">Notes</span>
							</NavLink>
							<NavLink to="/settings" className={linkClass}>
								<Settings className="h-4 w-4" />
								<span className="hidden sm:inline">Settings</span>
							</NavLink>
						</>
					)}

					<ThemeToggle />
					<span className="mx-1 h-6 w-px bg-border" />

					{loading ? null : isAuthenticated ? (
						<>
							<span className="hidden max-w-[8rem] truncate text-sm text-muted-foreground sm:inline">
								{user?.name}
							</span>
							<button type="button" onClick={handleLogout} className="nav-link">
								<LogOut className="h-4 w-4 text-destructive" />
								<span className="hidden sm:inline">Logout</span>
							</button>
						</>
					) : (
						<>
							<Link to="/login" className="nav-link">
								<LogIn className="h-4 w-4" />
								<span className="hidden sm:inline">Login</span>
							</Link>
							<Link
								to="/register"
								className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
							>
								<UserPlus className="h-4 w-4" />
								Register
							</Link>
						</>
					)}
				</div>
			</nav>
		</div>
	);
}
