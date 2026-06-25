import { useEffect, useState } from "react";
import TextInput from "@components/ui/TextInput";
import { Button } from "@components/ui/Button";
import { updateUserData } from "@backend/api";

export default function BasicInfoForm() {
	const [firstName, setFirstName] = useState("");
	const [lastName, setLastName] = useState("");
	const [email, setEmail] = useState("");
	const [status, setStatus] = useState<string | null>(null);

	useEffect(() => {
		fetch("/api/user/me", { credentials: "include" })
			.then((res) => (res.ok ? res.json() : null))
			.then((data) => {
				if (!data) return;
				setFirstName(data.firstName ?? "");
				setLastName(data.lastName ?? "");
				setEmail(data.email ?? "");
			})
			.catch(() => {});
	}, []);

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setStatus(null);
		try {
			await updateUserData({ FirstName: firstName, LastName: lastName, Email: email });
			setStatus("Saved.");
		} catch (err) {
			setStatus(err instanceof Error ? err.message : "Update failed.");
		}
	};

	return (
		<form onSubmit={handleSubmit} className="space-y-2 text-left">
			<h2 className="text-xl font-semibold">Basic info</h2>

			<TextInput id="firstName" label="First name" value={firstName}
				onChange={(e) => setFirstName(e.target.value)} />
			<TextInput id="lastName" label="Last name" value={lastName}
				onChange={(e) => setLastName(e.target.value)} />
			<TextInput id="email" label="Email" type="email" value={email}
				onChange={(e) => setEmail(e.target.value)} />

			{status && <p className="text-sm text-muted-foreground">{status}</p>}
			<Button type="submit">Update info</Button>
		</form>
	);
}
