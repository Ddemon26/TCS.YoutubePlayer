# Getting Started with TCS.YoutubePlayer

Welcome to TCS.YoutubePlayer, a Unity package that enables seamless YouTube video playback in your Unity projects with automatic tool management and cross-platform support.

<div class="welcome-section">
    <h1>🎬 TCS.YoutubePlayer</h1>
    <p class="lead">A powerful Unity package for YouTube video integration with automatic external tool management, supporting both streaming and download functionalities.</p>
    
    <div class="feature-grid">
        <div class="feature-card">
            <h3>🚀 Easy Integration</h3>
            <p>Drop-in prefab with minimal setup required. Get YouTube videos playing in minutes.</p>
        </div>
        <div class="feature-card">
            <h3>🔧 Auto Tool Management</h3>
            <p>Automatically downloads and manages yt-dlp and FFmpeg tools for you.</p>
        </div>
        <div class="feature-card">
            <h3>📱 Cross-Platform</h3>
            <p>Designed for Windows with planned support for macOS and Linux.</p>
        </div>
        <div class="feature-card">
            <h3>⚡ High Performance</h3>
            <p>Intelligent caching and async operations for smooth playback.</p>
        </div>
    </div>
</div>

## What You'll Learn

This documentation will guide you through:

- **Installation**: Setting up the package in your Unity project
- **Quick Start**: Playing your first YouTube video in under 5 minutes
- **Core Concepts**: Understanding the architecture and components
- **Advanced Usage**: Customizing behavior and extending functionality
- **Best Practices**: Performance optimization and troubleshooting

## Key Features

<div class="video-features">
    <div class="video-card">
        <h4>🎥 Video Playback</h4>
        <ul>
            <li>Direct YouTube URL support</li>
            <li>Streaming and download modes</li>
            <li>Quality selection</li>
            <li>Playback controls (play, pause, seek)</li>
            <li>Volume and speed control</li>
        </ul>
    </div>
    <div class="video-card">
        <h4>🛠️ Tool Management</h4>
        <ul>
            <li>Automatic yt-dlp downloads</li>
            <li>FFmpeg integration</li>
            <li>Version management</li>
            <li>Platform-specific binaries</li>
            <li>Manual override support</li>
        </ul>
    </div>
    <div class="video-card">
        <h4>💾 Smart Caching</h4>
        <ul>
            <li>URL result caching</li>
            <li>Expiration management</li>
            <li>Cache invalidation</li>
            <li>Storage optimization</li>
            <li>Configurable cache size</li>
        </ul>
    </div>
</div>

## System Requirements

- **Unity Version**: 2020.3 or newer
- **Platform**: Windows (full support), macOS/Linux (planned)
- **Internet Connection**: Required for initial tool downloads
- **Storage**: ~50MB for external tools (yt-dlp + FFmpeg)

## Architecture Overview

<div class="architecture-diagram">
    <div class="component-row">
        <div class="component-box main-controller">
            <h4>YoutubePlayer</h4>
            <p>Main controller component</p>
        </div>
    </div>
    <div class="component-row">
        <div class="component-box">
            <h5>URL Processing</h5>
            <p>YouTube URL parsing</p>
        </div>
        <div class="component-box">
            <h5>Tool Management</h5>
            <p>yt-dlp & FFmpeg</p>
        </div>
        <div class="component-box">
            <h5>Video Conversion</h5>
            <p>Format processing</p>
        </div>
    </div>
    <div class="component-row">
        <div class="component-box">
            <h5>Caching System</h5>
            <p>URL result cache</p>
        </div>
        <div class="component-box">
            <h5>UI Toolkit</h5>
            <p>Player interface</p>
        </div>
        <div class="component-box">
            <h5>Configuration</h5>
            <p>Settings management</p>
        </div>
    </div>
</div>

## Next Steps

<div class="next-steps">
    <div class="step-card">
        <h4>📦 Install Package</h4>
        <p>Add TCS.YoutubePlayer to your Unity project via Package Manager or manual installation.</p>
        <a href="#documents~/installation.md" class="btn-link">Installation Guide</a>
    </div>
    <div class="step-card">
        <h4>🚀 Quick Start</h4>
        <p>Follow our quick start guide to play your first YouTube video in minutes.</p>
        <a href="#documents~/quick-start.md" class="btn-link">Quick Start</a>
    </div>
    <div class="step-card">
        <h4>🏗️ Learn Architecture</h4>
        <p>Understand the system architecture and core components.</p>
        <a href="#documents~/core/overview.md" class="btn-link">System Overview</a>
    </div>
</div>

## Community & Support

- **GitHub Repository**: [TCS.YoutubePlayer](https://github.com/Ddemon26/TCS-YoutubePlayer)
- **Issues & Bug Reports**: Use GitHub Issues for bug reports and feature requests
- **Documentation**: This comprehensive guide covers all aspects of the system

Ready to get started? Let's [install the package](#documents~/installation.md) and play your first YouTube video!