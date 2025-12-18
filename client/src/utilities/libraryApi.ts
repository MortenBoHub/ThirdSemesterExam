// Temporary stub: the current OpenAPI spec does not generate LibraryClient/Book.
// Provide a minimal API so the UI can build and deploy. Replace with real client when backend adds endpoints.
export type Book = { id: string; title: string }

export const libraryApi = {
    async getBooks(_requestDto: unknown): Promise<Book[]> {
        return []
    }
}
