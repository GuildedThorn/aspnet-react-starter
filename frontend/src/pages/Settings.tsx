import BasicInfoForm from "@components/UserSettings/BasicInfoForm";
import SecurityKeysForm from "@components/UserSettings/SecurityKeysForm";

export default function Settings() {
	return (
		<div className="mx-auto max-w-2xl space-y-8 px-4 py-10">
			<h1 className="text-2xl font-bold">Settings</h1>

			<section className="panel p-6">
				<BasicInfoForm />
			</section>

			<section className="panel p-6">
				<SecurityKeysForm />
			</section>
		</div>
	);
}
