import EmptyFilter from "@/app/components/EmptyFilter";
import { SearchParams } from "@/types";

export default async function SignIn(props: { params: SearchParams }) {
    const params = await props.params
    return (
        <EmptyFilter
            title='You need to be logged in to do that'
            subtitle='Please click below to login'
            showLogin
            callbackUrl={params.callbackUrl}
            showReset={false}
        />            
    );
}