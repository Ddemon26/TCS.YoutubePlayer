class NavigationManager {
    constructor() {
        this.navigation = [];
    }

    async load() {
        try {
            const response = await fetch('documents~/navigation.json');
            if (response.ok) {
                this.navigation = await response.json();
            } else {
                this.generateDefaultNavigation();
            }
        } catch (error) {
            console.error('Failed to load navigation:', error);
            this.generateDefaultNavigation();
        }
    }

    generateDefaultNavigation() {
        this.navigation = [
            {
                title: "Getting Started",
                items: [
                    { title: "Introduction", path: "documents~/introduction.md" },
                    { title: "Installation", path: "documents~/installation.md" },
                    { title: "Quick Start", path: "documents~/quick-start.md" }
                ]
            },
            {
                title: "Language Reference",
                items: [
                    { title: "Lexical Analysis", path: "documents~/lexical.md" },
                    { title: "Source Text", path: "documents~/lexical.md#source-text" },
                    { title: "Character Set", path: "documents~/lexical.md#character-set" },
                    { title: "End of File", path: "documents~/lexical.md#end-of-file" },
                    { title: "End of Line", path: "documents~/lexical.md#end-of-line" },
                    { title: "White Space", path: "documents~/lexical.md#white-space" },
                    { title: "Comments", path: "documents~/lexical.md#comments" },
                    { title: "Tokens", path: "documents~/lexical.md#tokens" },
                    { title: "Identifiers", path: "documents~/lexical.md#identifiers" },
                    { title: "String Literals", path: "documents~/lexical.md#string-literals" },
                    { title: "Wysiwyg Strings", path: "documents~/lexical.md#wysiwyg-strings" },
                    { title: "Double Quoted Strings", path: "documents~/lexical.md#double-quoted-strings" },
                    { title: "Delimited Strings", path: "documents~/lexical.md#delimited-strings" },
                    { title: "Token Strings", path: "documents~/lexical.md#token-strings" },
                    { title: "Hex Strings", path: "documents~/lexical.md#hex-strings" },
                    { title: "String Postfix", path: "documents~/lexical.md#string-postfix" },
                    { title: "Character Literals", path: "documents~/lexical.md#character-literals" },
                    { title: "Integer Literals", path: "documents~/lexical.md#integer-literals" },
                    { title: "Floating Point Literals", path: "documents~/lexical.md#floating-point-literals" },
                    { title: "Keywords", path: "documents~/lexical.md#keywords" },
                    { title: "Special Tokens", path: "documents~/lexical.md#special-tokens" },
                    { title: "Special Token Sequences", path: "documents~/lexical.md#special-token-sequences" }
                ]
            },
            {
                title: "API Reference",
                items: [
                    { title: "Overview", path: "documents~/api/overview.md" },
                    { title: "Authentication", path: "documents~/api/authentication.md" },
                    { title: "API Keys", path: "documents~/api/auth/api-keys.md" }
                ]
            },
            {
                title: "Guides",
                items: [
                    { title: "Basic Usage", path: "documents~/guides/basic-usage.md" },
                    { title: "Advanced Features", path: "documents~/guides/advanced-features.md" }
                ]
            },
            {
                title: "Examples",
                items: [
                    { title: "Hello World (.dr)", path: "documents~/other-page.md" }
                ]
            },
            {
                title: "Community",
                items: [
                    { title: "Community Hub", path: "documents~/community/index.md" },
                    { title: "Discussion Forums", path: "documents~/community/forums.md" },
                    { title: "Chat & Support", path: "documents~/community/chat.md" },
                    { title: "Events & Meetups", path: "documents~/community/events.md" },
                    { title: "Community Guidelines", path: "documents~/community/guidelines.md" },
                    { title: "Contributing", path: "documents~/community/contributing.md" }
                ]
            },
            {
                title: "Resources",
                items: [
                    { title: "All Resources", path: "documents~/resources/index.md" }
                ]
            },
            {
                title: "Downloads",
                items: [
                    { title: "Latest Release", path: "documents~/downloads/index.md" }
                ]
            }
        ];
    }

    render(onItemClick) {
        const nav = document.getElementById('navigation');
        nav.innerHTML = '';

        this.navigation.forEach(section => {
            const sectionDiv = document.createElement('div');
            sectionDiv.className = 'nav-section';

            const sectionTitle = document.createElement('h3');
            sectionTitle.textContent = section.title;
            sectionDiv.appendChild(sectionTitle);

            const ul = document.createElement('ul');
            section.items.forEach(item => {
                this.renderNavItem(item, ul, onItemClick, 0);
            });

            sectionDiv.appendChild(ul);
            nav.appendChild(sectionDiv);
        });
    }

    renderNavItem(item, parentUl, onItemClick, depth) {
        const li = document.createElement('li');
        li.className = depth > 0 ? `nav-item-depth-${depth}` : 'nav-item';

        if (item.path) {
            // This is a leaf item with a path
            const a = document.createElement('a');
            a.href = `#${item.path}`;
            a.textContent = item.title;
            a.addEventListener('click', (e) => {
                e.preventDefault();
                onItemClick(item.path, a);
            });
            li.appendChild(a);
        } else if (item.items) {
            // This is a container item with sub-items
            const span = document.createElement('span');
            span.textContent = item.title;
            span.className = 'nav-section-title';
            li.appendChild(span);

            const subUl = document.createElement('ul');
            subUl.className = 'nav-subsection';
            item.items.forEach(subItem => {
                this.renderNavItem(subItem, subUl, onItemClick, depth + 1);
            });
            li.appendChild(subUl);
        }

        parentUl.appendChild(li);
    }

    updateActiveItem(activeLink) {
        document.querySelectorAll('.nav a').forEach(link => {
            link.classList.remove('active');
        });

        if (activeLink) {
            activeLink.classList.add('active');
        }
    }

    findItemByPath(path) {
        for (const section of this.navigation) {
            const result = this.findItemInSection(section.items, path);
            if (result) {
                return { item: result, section };
            }
        }
        return null;
    }

    findItemInSection(items, path) {
        for (const item of items) {
            if (item.path === path) {
                return item;
            }
            if (item.items) {
                const result = this.findItemInSection(item.items, path);
                if (result) {
                    return result;
                }
            }
        }
        return null;
    }

    getFirstItem() {
        if (this.navigation.length === 0) {
            return null;
        }
        
        const firstSection = this.navigation[0];
        if (firstSection.items.length === 0) {
            return null;
        }
        
        const firstItem = firstSection.items[0];
        if (firstItem.path) {
            return firstItem;
        }
        
        // If first item has sub-items, find the first item with a path
        return this.findFirstItemWithPath(firstItem.items);
    }

    findFirstItemWithPath(items) {
        for (const item of items) {
            if (item.path) {
                return item;
            }
            if (item.items) {
                const result = this.findFirstItemWithPath(item.items);
                if (result) {
                    return result;
                }
            }
        }
        return null;
    }
}

export default NavigationManager;