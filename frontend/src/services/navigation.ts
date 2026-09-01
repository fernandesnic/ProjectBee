type NavigateFn = (path: string) => void

let navigateRef: NavigateFn | null = null

export function setNavigator(fn: NavigateFn) {
    navigateRef = fn
}

export function navigateTo(path: string) {
    if (navigateRef) {
        navigateRef(path)
    } else {
        window.location.href = path
    }
}