import { startRegistration, startAuthentication } from "@simplewebauthn/browser";

async function asError(res: Response, fallback: string) {
	const text = await res.text().catch(() => "");
	return new Error(text || fallback);
}

/* ───────────────────────── Auth ───────────────────────── */

export async function register(username: string, password: string) {
	const res = await fetch("/api/auth/register", {
		method: "POST",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ username, password }),
		credentials: "include",
	});
	if (!res.ok) throw await asError(res, "Registration failed.");
	return res.json();
}

export async function logout() {
	const res = await fetch("/api/auth/logout", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
	});
	if (!res.ok) throw await asError(res, "Logout failed.");
	return res.json();
}

export async function updateUserData(userData: {
	FirstName?: string;
	LastName?: string;
	Email?: string;
}) {
	const res = await fetch("/api/user/updateData", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify(userData),
	});
	if (!res.ok) throw await asError(res, "Update failed.");
	return res.json();
}

/* ───────────────── WebAuthn (passkeys / security keys) ───────────────── */

export interface SecurityKey {
	id: string;
	nickname: string;
	createdAt: string;
	lastUsedAt: string;
}

export async function registerSecurityKey(nickname: string) {
	const begin = await fetch("/api/webauthn/register/begin", {
		method: "POST",
		credentials: "include",
	});
	if (!begin.ok) throw await asError(begin, "Couldn't start key registration.");
	const { id, optionsJson } = await begin.json();

	const attestation = await startRegistration({ optionsJSON: JSON.parse(optionsJson) });

	const finish = await fetch("/api/webauthn/register/finish", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ id, nickname, response: attestation }),
	});
	if (!finish.ok) throw await asError(finish, "Couldn't register that key.");
}

// Log in with a security key. Omit username for passwordless (discoverable key);
// pass a username to scope it as a second factor for that account.
export async function loginWithSecurityKey(username?: string) {
	const begin = await fetch("/api/webauthn/assert/begin", {
		method: "POST",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ username: username ?? null }),
	});
	if (!begin.ok) throw await asError(begin, "Couldn't start security-key login.");
	const { id, optionsJson } = await begin.json();

	const assertion = await startAuthentication({ optionsJSON: JSON.parse(optionsJson) });

	const finish = await fetch("/api/webauthn/assert/finish", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ id, response: assertion }),
	});
	if (!finish.ok) throw await asError(finish, "Security-key login failed.");
}

export async function listSecurityKeys(): Promise<SecurityKey[]> {
	const res = await fetch("/api/webauthn/credentials", { credentials: "include" });
	if (!res.ok) throw await asError(res, "Couldn't load security keys.");
	return res.json();
}

export async function deleteSecurityKey(id: string) {
	const res = await fetch(`/api/webauthn/credentials/${id}`, {
		method: "DELETE",
		credentials: "include",
	});
	if (!res.ok) throw await asError(res, "Couldn't remove that key.");
}

export async function getTwoFactorStatus(): Promise<{ enabled: boolean; hasKeys: boolean }> {
	const res = await fetch("/api/webauthn/two-factor", { credentials: "include" });
	if (!res.ok) throw await asError(res, "Couldn't load 2FA status.");
	return res.json();
}

export async function setTwoFactor(enabled: boolean) {
	const res = await fetch("/api/webauthn/two-factor", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ enabled }),
	});
	if (!res.ok) throw await asError(res, "Couldn't update 2FA.");
	return res.json();
}

/* ───────────────────────── Notes (example CRUD) ───────────────────────── */

export interface Note {
	id: string;
	title: string;
	body: string;
	createdAt: string;
	updatedAt: string;
}

export async function listNotes(): Promise<Note[]> {
	const res = await fetch("/api/notes", { credentials: "include" });
	if (!res.ok) throw await asError(res, "Couldn't load notes.");
	return res.json();
}

export async function createNote(title: string, body: string): Promise<Note> {
	const res = await fetch("/api/notes", {
		method: "POST",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ title, body }),
	});
	if (!res.ok) throw await asError(res, "Couldn't create note.");
	return res.json();
}

export async function updateNote(id: string, title: string, body: string): Promise<Note> {
	const res = await fetch(`/api/notes/${id}`, {
		method: "PUT",
		credentials: "include",
		headers: { "Content-Type": "application/json" },
		body: JSON.stringify({ title, body }),
	});
	if (!res.ok) throw await asError(res, "Couldn't update note.");
	return res.json();
}

export async function deleteNote(id: string): Promise<void> {
	const res = await fetch(`/api/notes/${id}`, {
		method: "DELETE",
		credentials: "include",
	});
	if (!res.ok) throw await asError(res, "Couldn't delete note.");
}
