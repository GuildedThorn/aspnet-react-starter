// Credentials posted to the register/login endpoints.
export type User = {
	username: string;
	password: string;
};

export type UserUpdateRequest = {
	FirstName?: string;
	LastName?: string;
	Email?: string;
};
