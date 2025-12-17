import * as React from "react";
import * as TabsPrimitive from "@radix-ui/react-tabs";
import { cn } from "./utils";

function Tabs(
    { className, ...props }: React.ComponentProps<typeof TabsPrimitive.Root>
)  {
    return (
        <TabsPrimitive.Root
            data-slot="tabs"
            className={cn("flex flex-col gap-2", className)}
            {...props}
            />
    );
}

function TabsList(
    { className, ...props }: React.ComponentProps<typeof TabsPrimitive.List>
) {
    return (
        <TabsPrimitive.List
            data-slot="tabs-list"
            className={cn(
                "flex w-full gap-2 bg-transparent p-0",
                className
            )}
            {...props}
        />
    );
}

function TabsTrigger(
    { className, ...props }: React.ComponentProps<typeof TabsPrimitive.Trigger>
) {
    return (
        <TabsPrimitive.Trigger
            data-slot="tabs-trigger"
            className={cn(
                "flex-1 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 transition-all",
                "hover:border-[#ed1c24] hover:text-[#ed1c24]",
                "data-[state=active]:border-[#ed1c24] data-[state=active]:bg-[#ed1c24]/10 data-[state=active]:text-[#ed1c24]",
                className
            )}
            {...props}
        />
    );
}

function TabsContent(
    { className, ...props }: React.ComponentProps<typeof TabsPrimitive.Content>
) {
    return (
        <TabsPrimitive.Content
            data-slot="tabs-content"
            className={cn("flex-1 outline-none", className)}
            {...props}
        />
    );
}

export { Tabs, TabsList, TabsTrigger, TabsContent };
    
