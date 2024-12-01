'use server'

import { auth } from "@/auth";

export async function getCurrentUser() {
    try {
        const session = await auth();
        
        if (!session) { return null; }
        
        return session.user;
    } catch(err) {
        console.log(err);
        return null
    }
}