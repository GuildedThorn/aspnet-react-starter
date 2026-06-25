export default function Footer() {
	return (
		<div className="mt-12 px-3 pb-3">
			<footer className="mx-auto max-w-5xl rounded-2xl border border-border bg-card/80 px-4 py-6 text-center text-sm text-muted-foreground shadow-sm backdrop-blur sm:px-6">
				&copy; {new Date().getFullYear()} App. Built with ASP.NET Core &amp; React.
			</footer>
		</div>
	);
}
