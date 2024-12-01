import Heading from "@/app/components/Heading";
import AuctionForm from "../../AuctionForm";
import { getDetailedViewData } from "@/app/actions/auctionActions";
import { Auction, SearchParams } from "@/types";


export default async function Update(props: { params: SearchParams }) {
    const params = await props.params;
    const data: Auction = await getDetailedViewData(params.id!);
    
    return (
        <div className='mx-auto max-w-[75%] shadow-lg p-10 bg-white rounded-lg'>
            <Heading title='Update your auction' subtitle='Please update the details of your car' />
            <AuctionForm auction={data}/>
        </div>
    )
}