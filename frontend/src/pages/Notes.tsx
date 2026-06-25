import { useCallback, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { Trash2 } from "lucide-react";
import { Button } from "@components/ui/Button";
import { Input } from "@components/ui/Input";
import { Textarea } from "@components/ui/TextArea";
import {
	createNote,
	deleteNote,
	listNotes,
	type Note,
} from "@backend/api";

export default function Notes() {
	const [notes, setNotes] = useState<Note[]>([]);
	const [title, setTitle] = useState("");
	const [body, setBody] = useState("");
	const [error, setError] = useState<string | null>(null);
	const [live, setLive] = useState(false);

	const refetch = useCallback(() => {
		listNotes()
			.then(setNotes)
			.catch((e) => setError(e instanceof Error ? e.message : "Failed to load notes."));
	}, []);

	useEffect(() => {
		refetch();
	}, [refetch]);

	// Live updates: the server broadcasts "NoteChanged" over SignalR whenever any
	// note is created/updated/deleted; just refetch when we hear it.
	const connRef = useRef<signalR.HubConnection | null>(null);
	useEffect(() => {
		const conn = new signalR.HubConnectionBuilder()
			.withUrl("/hub")
			.withAutomaticReconnect()
			.build();
		conn.on("NoteChanged", refetch);
		conn.onreconnected(() => setLive(true));
		conn.onclose(() => setLive(false));
		conn.start().then(() => setLive(true)).catch(() => setLive(false));
		connRef.current = conn;
		return () => {
			conn.stop();
		};
	}, [refetch]);

	const add = async (e: React.FormEvent) => {
		e.preventDefault();
		if (!title.trim()) return;
		setError(null);
		try {
			await createNote(title.trim(), body);
			setTitle("");
			setBody("");
			refetch(); // the broadcast also refreshes other clients
		} catch (err) {
			setError(err instanceof Error ? err.message : "Failed to create note.");
		}
	};

	const remove = async (id: string) => {
		try {
			await deleteNote(id);
			refetch();
		} catch (err) {
			setError(err instanceof Error ? err.message : "Failed to delete note.");
		}
	};

	return (
		<div className="mx-auto max-w-2xl px-4 py-10">
			<div className="mb-6 flex items-center justify-between">
				<h1 className="text-2xl font-bold">Notes</h1>
				<span className="flex items-center gap-2 text-xs text-muted-foreground">
					<span className={`h-2 w-2 rounded-full ${live ? "bg-green-500" : "bg-muted-foreground/40"}`} />
					{live ? "live" : "offline"}
				</span>
			</div>

			<form onSubmit={add} className="panel mb-8 space-y-3 p-4">
				<Input
					value={title}
					onChange={(e) => setTitle(e.target.value)}
					placeholder="Title"
					aria-label="Note title"
				/>
				<Textarea
					value={body}
					onChange={(e) => setBody(e.target.value)}
					placeholder="Write something…"
					rows={3}
					aria-label="Note body"
				/>
				<Button type="submit">Add note</Button>
			</form>

			{error && <p className="mb-4 text-sm text-destructive">{error}</p>}

			{notes.length === 0 ? (
				<p className="text-center text-sm text-muted-foreground">No notes yet.</p>
			) : (
				<ul className="space-y-3">
					{notes.map((n) => (
						<li key={n.id} className="panel flex items-start justify-between gap-3 p-4">
							<div className="min-w-0">
								<h3 className="truncate font-semibold">{n.title}</h3>
								{n.body && <p className="mt-1 whitespace-pre-wrap text-sm text-muted-foreground">{n.body}</p>}
							</div>
							<button
								type="button"
								onClick={() => remove(n.id)}
								aria-label="Delete note"
								className="shrink-0 rounded-md p-2 text-destructive hover:bg-muted"
							>
								<Trash2 className="h-4 w-4" />
							</button>
						</li>
					))}
				</ul>
			)}
		</div>
	);
}
