//Depracated/unused file
export type Book = { id: string; title: string }

export const libraryApi = {
    async getBooks(_requestDto: unknown): Promise<Book[]> {
        return []
    }
}
