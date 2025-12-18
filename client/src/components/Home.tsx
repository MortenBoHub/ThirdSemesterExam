import {Outlet, useNavigate} from "react-router-dom";

export default function Home() {

    const navigate = useNavigate();

    return <>
        
        <Outlet/>

    </>
}