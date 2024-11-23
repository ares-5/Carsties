'use client'

import {Dropdown, DropdownDivider, DropdownItem} from "flowbite-react";
import { User } from "next-auth";
import { useRouter } from "next/navigation";
import {HiCog, HiUser } from "react-icons/hi2";
import Link from "next/link";
import {AiFillCar, AiFillTrophy } from "react-icons/ai";
import {HiOutlineLogout} from "react-icons/hi";
import { signOut } from "next-auth/react";

type Props = {
    user: User
}

export default function UserActions({user}: Props) {
    const router = useRouter();

    return (
        <Dropdown inline label={`Welcome ${user.name}`}>
            <DropdownItem icon={HiUser}>
                <Link href='/'>
                    My Auctions
                </Link>
            </DropdownItem>   
            <DropdownItem icon={AiFillTrophy}>
                <Link href='/'>
                    Auctions Won
                </Link>
            </DropdownItem>    
            <DropdownItem icon={AiFillCar}>
                <Link href='/'>
                    Sell my car
                </Link>
            </DropdownItem>
            <DropdownItem icon={HiCog}>
                <Link href='/session'>
                    Session (dev only!)
                </Link>
            </DropdownItem>
            <DropdownDivider />
            <DropdownItem icon={HiOutlineLogout} onClick={() => signOut({redirectTo: '/'})}>
                Sign out
            </DropdownItem>
            <DropdownDivider />
        </Dropdown>
    );
}