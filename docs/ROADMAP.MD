# Roadmap

This roadmap describes engineering problems worth investigating.
It does not prescribe technologies.

## Initial

- Build the basic order flow.
- Understand the domain.
- Establish a behavioral baseline.

## Future Investigations

- What happens when multiple customers purchase the same stock concurrently?
- How does the system behave under increasing load?
- What happens when a database operation partially fails?
- How should users authenticate?
- How should permissions be represented?
- How can expensive reads be improved?
- How can failures be traced across an operation?
- How should independent business capabilities communicate?
- What happens when asynchronous messages are duplicated?
- What happens when a message cannot be published?
- How should external payment failures be handled?
- When does separating modules become valuable?
- When would independent deployment become valuable?
- How should multiple application instances coordinate?